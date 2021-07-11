using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using MediatR;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class StateChangedNotificationHandler : INotificationHandler<HomeAssistantClient.StateChangedNotification>
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IAutomationService _automationService;
        private readonly IEntityRegistry _entityRegistry;

        public StateChangedNotificationHandler(
            IAutomationRegistry automationRegistry,
            IEntityRegistry entityRegistry,
            IAutomationService automationService)
        {
            _automationRegistry = automationRegistry;
            _entityRegistry = entityRegistry;
            _automationService = automationService;
        }

        public async Task Handle(HomeAssistantClient.StateChangedNotification notification, CancellationToken cancellationToken)
        {
            // Update entities
            await _entityRegistry.UpdateEntityAsync(notification.EntityId, notification.NewState);

            var automations = _automationRegistry.GetAutomationsTrackingEntity(notification.EntityId);

            // Run automations
            foreach (var automation in automations)
            {
                await _automationService.EnqueueAutomationAsync(automation, Shared.Automation.AutomationRunInfo.StartReason.EntityChanged, $"Entity '{notification.EntityId}' changed.", notification.SourceEvent);
            }
        }
    }
}