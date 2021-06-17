using System;
using System.Linq;
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
        void RunAutomation(AutomationEntry automation);

        void StopAutomation(AutomationEntry automation);
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

        public void RunAutomation(AutomationEntry automation)
        {
            // TODO handle reentry policy
            Task.Run(async () =>
            {
                using var scope = _provider.CreateScope();
                using var cts = new CancellationTokenSource();

                var run = new AutomationRunInfo
                {
                    Started = DateTimeOffset.Now,
                    State = AutomationRunInfo.RunState.Running,
                    CancellationTokenSource = cts
                };

                AutomationRunContext.Current = new AutomationRunContext(cts.Token, scope.ServiceProvider);

                automation.AddRun(run);

                try
                {
                    await automation.Info.RunAutomation(scope.ServiceProvider, cts.Token);

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
                    run.CancellationTokenSource = null;
                }
            });
        }

        public void StopAutomation(AutomationEntry automation)
        {
            automation.Runs.FirstOrDefault()?.CancellationTokenSource?.Cancel();
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