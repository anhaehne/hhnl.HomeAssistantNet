using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRunner
    {
        void RunAutomation(AutomationEntry automationInfo);
    }

    public class AutomationRunner : IHostedService, IAutomationRunner
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IServiceProvider _provider;

        public AutomationRunner(IServiceProvider provider, IAutomationRegistry automationRegistry)
        {
            _provider = provider;
            _automationRegistry = automationRegistry;
        }

        public void RunAutomation(AutomationEntry automationInfo)
        {
            // TODO handle reentry policy
            Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();

                var run = new AutomationRunInfo
                {
                    Started = DateTimeOffset.Now,
                    State = AutomationRunInfo.RunState.Running,
                    CancellationTokenSource = new CancellationTokenSource()
                };

                AutomationRunContext.Current = new AutomationRunContext(run.CancellationTokenSource.Token, scope.ServiceProvider);
                
                automationInfo.AddRun(run);

                try
                {
                    await automationInfo.Info.RunAutomation(scope.ServiceProvider, default);

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

                    var cts = run.CancellationTokenSource;
                    run.CancellationTokenSource = null;
                    cts?.Dispose();
                }
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            foreach (var runOnStartAutomations in _automationRegistry.Automations.Values)
            {
                RunAutomation(runOnStartAutomations);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}