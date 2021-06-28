using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications
{
    public class HubConnectionAddedNotification : INotification
    {
        public HubConnectionAddedNotification(string connectionId, bool isRemote)
        {
            ConnectionId = connectionId;
            IsRemote = isRemote;
        }

        private string ConnectionId { get; }

        private bool IsRemote { get; }

        public class ProcessInfoHandler : INotificationHandler<HubConnectionAddedNotification>
        {
            private readonly ILogger<ProcessInfoHandler> _logger;
            private readonly IManagementHubCallService _managementHubCallService;
            private readonly IMediator _mediator;
            private readonly IHubContext<SupervisorApiHub, ISupervisorApiClient> _supervisorApiHub;
            private readonly IAutomationsHostService _hostService;

            public ProcessInfoHandler(
                IManagementHubCallService managementHubCallService,
                IMediator mediator,
                ILogger<ProcessInfoHandler> logger,
                IAutomationsHostService hostService,
                IHubContext<SupervisorApiHub, ISupervisorApiClient> supervisorApiHub)
            {
                _managementHubCallService = managementHubCallService;
                _mediator = mediator;
                _logger = logger;
                _hostService = hostService;
                _supervisorApiHub = supervisorApiHub;
            }

            public async Task Handle(HubConnectionAddedNotification notification, CancellationToken cancellationToken)
            {
                _logger.LogDebug($"Got new connection {notification.ConnectionId}.");
                var previousConnection = _managementHubCallService.DefaultConnection;

                _managementHubCallService.DefaultConnection = new ConnectionInfo(notification.ConnectionId,
                    notification.IsRemote,
                    ArraySegment<AutomationInfoDto>.Empty, false);

                // Send intermediate info
                await _supervisorApiHub.Clients.All.OnConnectionChanged(_managementHubCallService.DefaultConnection);
                
                _logger.LogDebug($"Connection {notification.ConnectionId} is now the default connection.");

                if (previousConnection is not null)
                {
                    _logger.LogDebug($"Stopping previous connection {previousConnection}.");

                    // Close previous
                    await _mediator.Send(new StopProcessRequest(previousConnection.Id), cancellationToken);
                }
                
                _logger.LogDebug($"Getting new automations.");
                
                // Load automations
                var automations = await _hostService.GetAutomationsAsync();

                _logger.LogDebug($"Got {automations.Count} automations.");
                
                _managementHubCallService.DefaultConnection.Automations = automations;
                _managementHubCallService.DefaultConnection.IsComplete = true;
                
                // Send new connection to all web clients
                await _supervisorApiHub.Clients.All.OnConnectionChanged(_managementHubCallService.DefaultConnection);
            }
        }
    }
}