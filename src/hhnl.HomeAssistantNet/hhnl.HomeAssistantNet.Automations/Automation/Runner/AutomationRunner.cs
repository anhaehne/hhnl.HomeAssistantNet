using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public abstract class AutomationRunner
    {
        private readonly IServiceProvider _provider;

        protected AutomationRunner(AutomationEntry entry, IServiceProvider provider)
        {
            Entry = entry;
            _provider = provider;
        }

        protected AutomationEntry Entry { get; }

        public abstract Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs);

        public virtual void Start()
        {
        }

        public virtual Task StopAsync()
        {
            return Task.CompletedTask;
        }

        protected AutomationRunInfo CreateAutomationRun(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs,
            AutomationRunInfo.RunState initialState = AutomationRunInfo.RunState.Running)
        {
            var run = new AutomationRunInfo
            {
                Started = DateTimeOffset.Now,
                State = initialState,
                CancellationTokenSource = new CancellationTokenSource(),
                Reason = reason,
                ChangedEntity = changedEntity
            };

            run.Start = () =>
            {
                run.Task = Task.Run(async () =>
                {
                    startTcs?.TrySetResult();
                    using var scope = _provider.CreateScope();

                    AutomationRunContext.Current =
                        new AutomationRunContext(run.CancellationTokenSource.Token, scope.ServiceProvider, run);

                    try
                    {
                        await Entry.Info.RunAutomation(scope.ServiceProvider, run.CancellationTokenSource.Token);

                        run.State = run.CancellationTokenSource.Token.IsCancellationRequested
                            ? AutomationRunInfo.RunState.Cancelled
                            : AutomationRunInfo.RunState.Completed;
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
            };
            
            return run;
        }
    }
}