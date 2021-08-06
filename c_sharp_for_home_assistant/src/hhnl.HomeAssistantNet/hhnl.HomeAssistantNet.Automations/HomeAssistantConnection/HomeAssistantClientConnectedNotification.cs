using hhnl.HomeAssistantNet.Automations.Automation;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public class HomeAssistantClientConnectedNotification : INotification
    {
        private HomeAssistantClientConnectedNotification() { }

        public static HomeAssistantClientConnectedNotification Instance { get; } = new HomeAssistantClientConnectedNotification();

        public class Handler : INotificationHandler<HomeAssistantClientConnectedNotification>
        {
            public Handler(IAutomationService automationService)
            {
                _automationService = automationService;
            }

            private static readonly List<AutomationEntry> _automations = new List<AutomationEntry>();
            private readonly IAutomationService _automationService;

            public static void RegisterAutomation(AutomationEntry automation)
            {
                _automations.Add(automation);
            }

            public async Task Handle(HomeAssistantClientConnectedNotification notification, CancellationToken cancellationToken)
            {
                foreach (var automation in _automations)
                {
                    await _automationService.EnqueueAutomationAsync(automation, Shared.Automation.AutomationRunInfo.StartReason.RunOnConnect);
                }
            }
        }
    }
}
