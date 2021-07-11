using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class CancelPreviousAutomationRunner : QueueLatestAutomationRunner
    {
        public CancelPreviousAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot)
        {
            Entry.LatestRun?.CancellationTokenSource?.Cancel();
            return base.EnqueueAsync(reason, reasonMessage, startTcs, snapshot);
        }
    }
}