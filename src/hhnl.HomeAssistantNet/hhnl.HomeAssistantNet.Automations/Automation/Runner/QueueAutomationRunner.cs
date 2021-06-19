using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class QueueAutomationRunner : AutomationRunner
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly
            Channel<(AutomationRunInfo.StartReason Reason, string? ChangedEntity,
                TaskCompletionSource<bool>? StartTCS)> _runs = Channel
                .CreateBounded<(AutomationRunInfo.StartReason Reason, string? ChangedEntity,
                    TaskCompletionSource<bool>? StartTCS)>(1);

        private Task _runTask = Task.CompletedTask;

        public QueueAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override void Start()
        {
            _runTask = Run();
        }

        public override Task StopAsync()
        {
            _cts.Cancel();
            Entry.LatestRun?.CancellationTokenSource?.Cancel();
            return _runTask;
        }

        public override Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs)
        {
            if (!_runs.Writer.TryWrite((reason, changedEntity, startTcs)))
                startTcs?.TrySetResult(false);

            return Task.CompletedTask;
        }

        private async Task Run()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var next = await _runs.Reader.ReadAsync(_cts.Token);
                await StartAutomation(next.Reason, next.ChangedEntity, next.StartTCS).Task;
            }
        }
    }
}