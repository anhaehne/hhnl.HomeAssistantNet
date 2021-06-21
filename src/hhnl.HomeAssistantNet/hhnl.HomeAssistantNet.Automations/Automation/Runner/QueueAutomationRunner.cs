using System;
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
            Channel<AutomationRunInfo> _runs = Channel.CreateBounded<AutomationRunInfo>(1);

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

        public override Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs)
        {
            var run = CreateAutomationRun(reason, changedEntity, startTcs, AutomationRunInfo.RunState.WaitingInQueue);

            if (!_runs.Writer.TryWrite(run))
            {
                startTcs?.TrySetResult();
                return Task.CompletedTask;
            }
            
            Entry.AddRun(run);
            return Task.CompletedTask;
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