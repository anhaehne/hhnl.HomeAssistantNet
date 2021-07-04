using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class QueueAutomationRunner : AutomationRunner
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly
            Channel<AutomationRunInfo> _runs = Channel.CreateUnbounded<AutomationRunInfo>();

        private Task _runTask = Task.CompletedTask;

        public QueueAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override void Start()
        {
            _runTask = Run();
        }

        public override async Task StopAsync()
        {
            _cts.Cancel();
            Entry.LatestRun?.CancellationTokenSource?.Cancel();

            await _runTask.IgnoreCancellationAsync();
        }

        public override async Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot)
        {
            var run = CreateAutomationRun(reason, changedEntity, startTcs, snapshot, AutomationRunInfo.RunState.WaitingInQueue);

            await _runs.Writer.WriteAsync(run);
            Entry.AddRun(run);
        }

        private async Task Run()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var next = await _runs.Reader.ReadAsync(_cts.Token);
                next.Start();
                next.State = AutomationRunInfo.RunState.Running;
                await next.Task;
            }
        }
    }
}