using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications
{
    public class HubConnectionClosedNotification : INotification
    {
        public HubConnectionClosedNotification(string connectionId)
        {
            ConnectionId = connectionId;
        }

        private string ConnectionId { get; }


        public class HubCallUpdateHandler : INotificationHandler<HubConnectionClosedNotification>
        {
            private readonly IManagementHubCallService _managementHubCallService;

            public HubCallUpdateHandler(IManagementHubCallService managementHubCallService)
            {
                _managementHubCallService = managementHubCallService;
            }

            public Task Handle(HubConnectionClosedNotification notification, CancellationToken cancellationToken)
            {
                _managementHubCallService.RemoveConnection(notification.ConnectionId);
                return Task.CompletedTask;
            }
        }
    }
}