using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;
using Microsoft.Extensions.Logging;

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

            public Handler(IBuildService buildService, ILogger<Handler> logger)
            {
                _buildService = buildService;
                _logger = logger;
            }

            public async Task Handle(NoConnectionNotification notification, CancellationToken cancellationToken)
            {
                _logger.LogDebug("No client connections. Starting new instance.");
                
                await _buildService.WaitForBuildAndDeployAsync();

                _buildService.RunDeployedApplication();
            }
        }
    }
}