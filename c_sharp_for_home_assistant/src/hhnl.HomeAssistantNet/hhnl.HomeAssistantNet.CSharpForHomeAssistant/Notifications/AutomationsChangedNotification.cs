using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications
{
    public class AutomationsChangedNotification : INotification
    {
        public AutomationsChangedNotification(IReadOnlyCollection<AutomationInfoDto> automations)
        {
            Automations = automations;
        }

        public IReadOnlyCollection<AutomationInfoDto> Automations { get; }

        public class Handler : INotificationHandler<AutomationsChangedNotification>
        {
            private readonly IManagementHubCallService _managementHubCallService;
            private readonly IHubContext<SupervisorApiHub, ISupervisorApiClient> _supervisorHub;

            public Handler(
                IHubContext<SupervisorApiHub, ISupervisorApiClient> supervisorHub,
                IManagementHubCallService managementHubCallService)
            {
                _supervisorHub = supervisorHub;
                _managementHubCallService = managementHubCallService;
            }

            public async Task Handle(AutomationsChangedNotification notification, CancellationToken cancellationToken)
            {
                if (_managementHubCallService.DefaultConnection is null)
                    return;

                // Update the default connection.
                _managementHubCallService.DefaultConnection.Automations = notification.Automations;

                // Send new connection to all web clients
                await _supervisorHub.Clients.All.OnConnectionChanged(_managementHubCallService.DefaultConnection);
            }
        }
    }
}