using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications
{
    public class NoConnectionNotification : INotification
    {
        public static readonly NoConnectionNotification Instance = new();

        private NoConnectionNotification()
        {
        }

        public class Handler : INotificationHandler<NoConnectionNotification>
        {
            private readonly IBuildService _buildService;
            private readonly ILogger<Handler> _logger;
            private readonly IHubContext<SupervisorApiHub, ISupervisorApiClient> _supervisorApiHub;

            public Handler(IBuildService buildService, ILogger<Handler> logger, IHubContext<SupervisorApiHub, ISupervisorApiClient> supervisorApiHub)
            {
                _buildService = buildService;
                _logger = logger;
                _supervisorApiHub = supervisorApiHub;
            }

            public async Task Handle(NoConnectionNotification notification, CancellationToken cancellationToken)
            {
                await _supervisorApiHub.Clients.All.OnConnectionChanged(null);
                
                _logger.LogDebug("No client connections. Starting new instance.");
                
                await _buildService.WaitForBuildAndDeployAsync();

                _buildService.RunDeployedApplication();
            }
        }
    }
}