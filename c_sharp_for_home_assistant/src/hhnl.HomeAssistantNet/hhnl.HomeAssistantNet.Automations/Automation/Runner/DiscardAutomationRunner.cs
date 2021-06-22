using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class DiscardAutomationRunner : AutomationRunner
    {
        private Task _currentRun = Task.CompletedTask;
        private readonly SemaphoreSlim _semaphore = new(1);

        public DiscardAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override async Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs)
        {
            await _semaphore.WaitAsync();

            try
            {
                // If the current execution hasn't finished, we discard the run.
                if (!_currentRun.IsCompleted)
                {
                    startTcs?.TrySetResult();
                    return;
                }

                var run = CreateAutomationRun(reason, changedEntity, startTcs);
                
                Entry.AddRun(run);
                run.Start();
                
                _currentRun = run.Task;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override Task StopAsync()
        {
            Entry.LatestRun?.CancellationTokenSource?.Cancel();
            return _currentRun;
        }
    }
}