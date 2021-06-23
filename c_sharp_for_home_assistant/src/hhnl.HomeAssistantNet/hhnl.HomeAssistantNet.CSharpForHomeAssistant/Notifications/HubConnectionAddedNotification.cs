using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications
{
    public class HubConnectionAddedNotification : INotification
    {
        public HubConnectionAddedNotification(string connectionId)
        {
            ConnectionId = connectionId;
        }

        private string ConnectionId { get; }
        
        public class ProcessInfoHandler : INotificationHandler<HubConnectionAddedNotification>
        {
            private readonly IHubCallService _hubCallService;
            private readonly IMediator _mediator;
            private readonly ILogger<ProcessInfoHandler> _logger;

            public ProcessInfoHandler(IHubCallService hubCallService, IMediator mediator, ILogger<ProcessInfoHandler> logger)
            {
                _hubCallService = hubCallService;
                _mediator = mediator;
                _logger = logger;
            }


            public async Task Handle(HubConnectionAddedNotification notification, CancellationToken cancellationToken)
            {
                _logger.LogDebug($"Got new connection {notification.ConnectionId}. Getting process info.");
                var previousConnection = _hubCallService.DefaultConnection;
                
                _hubCallService.DefaultConnection = notification.ConnectionId;
                
                _logger.LogDebug($"Connection {notification.ConnectionId} is now the default connection.");

                if (string.IsNullOrEmpty(previousConnection))
                    return;
                
                _logger.LogDebug($"Stopping previous connection {previousConnection}.");
                // Close previous
                await _mediator.Send(new StopProcessRequest(previousConnection), cancellationToken);
            }
        }
    }
}