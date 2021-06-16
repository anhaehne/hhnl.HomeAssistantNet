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
        private readonly IAutomationRunner _automationRunner;
        private readonly IEntityRegistry _entityRegistry;

        public StateChangedNotificationHandler(
            IAutomationRegistry automationRegistry,
            IEntityRegistry entityRegistry,
            IAutomationRunner automationRunner)
        {
            _automationRegistry = automationRegistry;
            _entityRegistry = entityRegistry;
            _automationRunner = automationRunner;
        }

        public async Task Handle(HomeAssistantClient.StateChangedNotification notification, CancellationToken cancellationToken)
        {
            // Update entities
            await _entityRegistry.UpdateEntityAsync(notification.EntityId, notification.NewState);

            // Run automations
            var automations = _automationRegistry.GetAutomationsDependingOn(notification.EntityId);

            if (!automations.Any())
                return;

            foreach (var automation in automations)
            {
                _automationRunner.RunAutomation(automation);
            }
        }
    }
}