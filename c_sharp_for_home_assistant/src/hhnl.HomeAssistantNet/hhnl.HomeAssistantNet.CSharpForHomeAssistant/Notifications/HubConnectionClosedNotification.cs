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


        public class ProcessUpdateHandler : INotificationHandler<HubConnectionClosedNotification>
        {
            private readonly IProcessManager _processManager;

            public ProcessUpdateHandler(IProcessManager processManager)
            {
                _processManager = processManager;
            }

            public Task Handle(HubConnectionClosedNotification notification, CancellationToken cancellationToken)
            {
                _processManager.RemoveProcess(notification.ConnectionId);
                return Task.CompletedTask;
            }
        }

        public class HubCallUpdateHandler : INotificationHandler<HubConnectionClosedNotification>
        {
            private readonly IHubCallService _hubCallService;

            public HubCallUpdateHandler(IHubCallService hubCallService)
            {
                _hubCallService = hubCallService;
            }

            public Task Handle(HubConnectionClosedNotification notification, CancellationToken cancellationToken)
            {
                _hubCallService.RemoveConnection(notification.ConnectionId);
                return Task.CompletedTask;
            }
        }
    }
}