using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public abstract class AutomationRunner
    {
        protected AutomationEntry Entry { get; }

        private readonly IServiceProvider _provider;

        protected AutomationRunner(AutomationEntry entry, IServiceProvider provider)
        {
            Entry = entry;
            _provider = provider;
        }

        public abstract Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs);

        public virtual void Start()
        {
        }

        public virtual Task StopAsync() => Task.CompletedTask;
        
        protected AutomationRunInfo StartAutomation(AutomationRunInfo.StartReason reason, string? changedEntity, TaskCompletionSource<bool>? startTcs)
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
                using var scope = _provider.CreateScope();
                
                AutomationRunContext.Current = new AutomationRunContext(run.CancellationTokenSource.Token, scope.ServiceProvider, run);

                try
                {
                    await Entry.Info.RunAutomation(scope.ServiceProvider, run.CancellationTokenSource.Token);

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

            Entry.AddRun(run);
            
            startTcs?.TrySetResult(false);

            return run;
        }
    }
}