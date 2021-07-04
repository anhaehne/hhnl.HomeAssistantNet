using System;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public interface IAutomationRunnerFactory
    {
        AutomationRunner CreateRunnerFor(AutomationEntry entry);
    }

    public class AutomationRunnerFactory : IAutomationRunnerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public AutomationRunnerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AutomationRunner CreateRunnerFor(AutomationEntry entry)
        {
            return entry.Info.ReentryPolicy switch
            {
                ReentryPolicy.Allow => new AllowAutomationRunner(entry, _serviceProvider),
                ReentryPolicy.Discard => new DiscardAutomationRunner(entry, _serviceProvider),
                ReentryPolicy.Queue => new QueueAutomationRunner(entry, _serviceProvider),
                ReentryPolicy.QueueLatest => new QueueLatestAutomationRunner(entry, _serviceProvider),
                ReentryPolicy.CancelPrevious => new CancelPreviousAutomationRunner(entry, _serviceProvider),
                _ => throw new ArgumentOutOfRangeException(nameof(entry))
            };
        }
    }
}