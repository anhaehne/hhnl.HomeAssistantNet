using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRunner
    {
        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        /// <param name="changedEntity">The entity who's changes caused this automation to be enqueued.</param>
        Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity);
        
        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        Task EnqueueAutomationForManualStartAsync(AutomationEntry automation);

        Task StopAutomationAsync(AutomationEntry automation);
    }

    public class AutomationRunner : IHostedService, IAutomationRunner
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IServiceProvider _provider;

        private readonly Channel<(AutomationEntry Info, AutomationRunInfo.StartReason Reason, string? ChangedEntity, TaskCompletionSource<bool>? StartTCS)>
            _runQueue = Channel
                .CreateUnbounded<(AutomationEntry Info, AutomationRunInfo.StartReason Reason, string? ChangedEntity, TaskCompletionSource<bool>? StartTCS)>();
        
        private Task _runTask = Task.CompletedTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public AutomationRunner(IServiceProvider provider, IAutomationRegistry automationRegistry)
        {
            _provider = provider;
            _automationRegistry = automationRegistry;
        }

        public async Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity)
        {
            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.DenyChildAttach);
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.EntityChanged, changedEntity, tcs);
            await tcs.Task;
        }

        public async Task EnqueueAutomationForManualStartAsync(AutomationEntry automation)
        {
            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.DenyChildAttach);
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.Manual, null, tcs);
            await tcs.Task;
        }

        public Task StopAutomationAsync(AutomationEntry automation)
        {
            var run = automation.LatestRun;
            if(run is null || run.State != AutomationRunInfo.RunState.Running)
                return Task.CompletedTask;
            
            run.CancellationTokenSource?.Cancel();
            return run.Task;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            foreach (var runOnStartAutomations in _automationRegistry.Automations.Values.Where(a => a.Info.RunOnStart))
            {
                await EnqueueAutomationRunAsync(runOnStartAutomations, AutomationRunInfo.StartReason.Manual, null, null);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _runTask = RunAsync(_cancellationTokenSource.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource?.Cancel();
            return Task.WhenAny(_runTask, Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        }

        private async Task EnqueueAutomationRunAsync(
            AutomationEntry entry,
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs)
        {

            switch (entry.Info.ReentryPolicy)
            {
                case ReentryPolicy.Discard:
                    startTcs?.SetResult(false);
                    return;
                case ReentryPolicy.CancelPrevious:
                    entry.LatestRun?.CancellationTokenSource?.Cancel();
                    break;
                case ReentryPolicy.Queue:
                    if (entry.LatestRun is not null &&
                        entry.LatestRun.State == AutomationRunInfo.RunState.WaitingInQueue)
                    {
                        // When the automation run should be queued but there is already run waiting in queue, we discard this run.
                        startTcs?.SetResult(false);
                        return;
                    }

                    break;
                    
            }
            
            await _runQueue.Writer.WriteAsync((entry, reason, changedEntity, startTcs));
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var entry = await _runQueue.Reader.ReadAsync(cancellationToken);
                RunAutomationInternal(entry.Info, entry.Reason, entry.ChangedEntity, entry.StartTCS);
            }
        }

        private async Task WaitForAutomationRunAsync(AutomationRunInfo? run)
        {
            if (run is null || run.State != AutomationRunInfo.RunState.Running)
                return;

            // TODO: make queue mechanism thread safe
            
            run.State = AutomationRunInfo.RunState.WaitingInQueue;

            try
            {
                await run.Task;
            }
            catch (Exception)
            {
                // We don't care about the exceptions of the previous runs.
            }

            run.State = AutomationRunInfo.RunState.Running;
        }
        
        private void RunAutomationInternal(AutomationEntry automation, AutomationRunInfo.StartReason reason, string? changedEntity, TaskCompletionSource<bool>? startTcs)
        {
            var run = new AutomationRunInfo
            {
                Started = DateTimeOffset.Now,
                State = AutomationRunInfo.RunState.Running,
                CancellationTokenSource = new CancellationTokenSource(),
                Reason = reason,
                ChangedEntity = changedEntity
            };
            
            run.Task = Task.Run(async () =>
            {
                if (automation.Info.ReentryPolicy == ReentryPolicy.Queue)
                    await WaitForAutomationRunAsync(automation.LatestRun);
                
                using var scope = _provider.CreateScope();
                
                AutomationRunContext.Current = new AutomationRunContext(run.CancellationTokenSource.Token, scope.ServiceProvider, run);

                try
                {
                    await automation.Info.RunAutomation(scope.ServiceProvider, run.CancellationTokenSource.Token);

                    run.State = AutomationRunInfo.RunState.Completed;
                }
                catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                {
                    run.State = AutomationRunInfo.RunState.Cancelled;
                }
                catch (Exception e)
                {
                    run.State = AutomationRunInfo.RunState.Error;
                    run.Error = e.ToString();
                }
                finally
                {
                    run.Ended = DateTimeOffset.Now;
                    run.CancellationTokenSource.Dispose();
                    run.CancellationTokenSource = null;
                }
            });

            automation.AddRun(run);
            
            startTcs?.TrySetResult(false);
        }
    }
}