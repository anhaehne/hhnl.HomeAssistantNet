using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests
{
    public class StopProcessRequest : IRequest
    {
        public StopProcessRequest(string connectionId)
        {
            ConnectionId = connectionId;
        }

        private string ConnectionId { get; }

        public class Handler : IRequestHandler<StopProcessRequest>
        {
            private readonly IHubCallService _callService;
            private readonly IOptions<SupervisorConfig> _config;

            public Handler(IHubCallService callService, IOptions<SupervisorConfig> config)
            {
                _callService = callService;
                _config = config;
            }

            public async Task<Unit> Handle(StopProcessRequest request, CancellationToken cancellationToken)
            {
                try
                {
                    await _callService.CallService<bool>((l, client) => client.Shutdown(), connectionId: request.ConnectionId);
                }
                catch (TaskCanceledException)
                {
                    // Expected. The called client might not have enough time to answer the shutdown request.
                }

                return Unit.Value;
            }

            public class UnableToStopProcessesException : Exception
            {
                public UnableToStopProcessesException(int processId) : base($"Unable to stop process '{processId}'.")
                {
                }
            }
        }
    }
}