using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class EventFiredNotificationNotifcationHandler : INotificationHandler<HomeAssistantClient.EventFiredNotification>
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IAutomationService _automationService;

        public EventFiredNotificationNotifcationHandler(
            IAutomationRegistry automationRegistry,
            IAutomationService automationService)
        {
            _automationRegistry = automationRegistry;
            _automationService = automationService;
        }

        public async Task Handle(HomeAssistantClient.EventFiredNotification notification, CancellationToken cancellationToken)
        {
            var automations = _automationRegistry.GetAutomationsTrackingEntity(Events.Any.UniqueId);

            // Run automations
            foreach (var automation in automations)
            {
                await _automationService.EnqueueAutomationForEventFiredAsync(automation, notification.Event);
            }
        }
    }
}
