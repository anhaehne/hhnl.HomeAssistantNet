using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;
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
            private readonly IOptions<SupervisorConfig> _config;

            public Handler(IBuildService buildService, ILogger<Handler> logger, IOptions<SupervisorConfig> config)
            {
                _buildService = buildService;
                _logger = logger;
                _config = config;
            }

            public async Task Handle(NoConnectionNotification notification, CancellationToken cancellationToken)
            {
                if (_config.Value.SuppressAutomationDeploy)
                    return;
                
                _logger.LogDebug("No client connections. Starting new instance.");
                
                await _buildService.WaitForBuildAndDeployAsync();

                _buildService.RunDeployedApplication();
            }
        }
    }
}