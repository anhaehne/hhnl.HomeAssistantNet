using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class QueueAutomationRunner : AutomationRunner
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly
            Channel<AutomationRunInfo> _runs = Channel.CreateUnbounded<AutomationRunInfo>();
        private readonly new ILogger<QueueAutomationRunner> _logger;

        private Task _runTask = Task.CompletedTask;

        public QueueAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
            _logger = provider.GetRequiredService<ILogger<QueueAutomationRunner>>();
        }

        public override void Start()
        {
            _runTask = Run();
            _runTask.ContinueWith(task =>
            {
                if (!_cts.IsCancellationRequested)
                    _logger.LogError("The run task has completed even though the runner hasn't been stopped.");
            });
        }

        public override async Task StopAsync()
        {
            _cts.Cancel();
            Entry.LatestRun?.CancellationTokenSource?.Cancel();

            await _runTask.IgnoreCancellationAsync();
        }

        public override async Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot)
        {
            var run = CreateAutomationRun(reason, reasonMessage, startTcs, snapshot, AutomationRunInfo.RunState.WaitingInQueue);

            await PublishRunChangedAsync(run);

            await _runs.Writer.WriteAsync(run);
            Entry.AddRun(run);

            startTcs?.TrySetResult();
        }

        private async Task Run()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var next = await _runs.Reader.ReadAsync(_cts.Token);

                // Skip cancelled runs.
                if (next.State == AutomationRunInfo.RunState.Cancelled)
                    continue; 

                next.Start();
                next.State = AutomationRunInfo.RunState.Running;
                await next.Task;
            }
        }
    }
}