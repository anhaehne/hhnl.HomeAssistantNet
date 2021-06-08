using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRunner
    {
        Task RunAutomationAsync(AutomationRunInfo automationInfo);
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

        public async Task RunAutomationAsync(AutomationRunInfo automationInfo)
        {
            using var scope = _provider.CreateScope();
            await automationInfo.Info.RunAutomation(scope.ServiceProvider);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            foreach (var runOnStartAutomations in _automationRegistry.Automations.Where(x => x.Info.RunOnStart))
            {
                await RunAutomationAsync(runOnStartAutomations);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}