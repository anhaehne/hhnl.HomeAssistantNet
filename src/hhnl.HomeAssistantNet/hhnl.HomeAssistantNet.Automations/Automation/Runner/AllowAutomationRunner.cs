using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class AllowAutomationRunner : AutomationRunner
    {
        private readonly ConcurrentDictionary<AutomationRunInfo, bool> _runs = new();

        public AllowAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs)
        {
            var run = StartAutomation(reason, changedEntity, startTcs);
            _runs.TryAdd(run, false);

            // Remove run when it is completed.
            run.Task.ContinueWith(task => _runs.TryRemove(run, out _));

            return Task.CompletedTask;
        }

        public override Task StopAsync()
        {
            var runs = _runs.Keys;

            foreach (var run in runs)
            {
                run.CancellationTokenSource?.Cancel();
            }

            return Task.WhenAll(runs.Select(r => r.Task));
        }
    }
}