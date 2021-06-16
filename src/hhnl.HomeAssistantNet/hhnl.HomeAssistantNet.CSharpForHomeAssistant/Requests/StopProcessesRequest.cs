using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests
{
    public class StopProcessesRequest : IRequest
    {
        public StopProcessesRequest(string exceptConnectionId)
        {
            ExceptConnectionId = exceptConnectionId;
        }

        private StopProcessesRequest()
        {
        }

        private string? ExceptConnectionId { get; }

        public static StopProcessesRequest All { get; } = new();

        public class Handler : IRequestHandler<StopProcessesRequest>
        {
            private readonly IMediator _mediator;
            private readonly IProcessManager _processManager;

            public Handler(IMediator mediator, IProcessManager processManager)
            {
                _mediator = mediator;
                _processManager = processManager;
            }

            public async Task<Unit> Handle(StopProcessesRequest request, CancellationToken cancellationToken)
            {
                var processes = _processManager.Processes;

                if (!string.IsNullOrEmpty(request.ExceptConnectionId))
                    processes = processes.Where(p => p.ConnectionId == request.ExceptConnectionId).ToList();

                await Task.WhenAll(processes.Select(p =>
                    _mediator.Send(new StopProcessRequest(p.ConnectionId), cancellationToken)));
                return Unit.Value;
            }
        }
    }
}