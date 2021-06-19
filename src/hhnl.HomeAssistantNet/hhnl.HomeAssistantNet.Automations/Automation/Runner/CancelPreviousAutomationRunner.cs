using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class CancelPreviousAutomationRunner : QueueAutomationRunner
    {
        public CancelPreviousAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs)
        {
            Entry.LatestRun?.CancellationTokenSource?.Cancel();
            return base.EnqueueAsync(reason, changedEntity, startTcs);
        }
    }
}