using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests
{
    public class AddProcessConnectionRequest : IRequest
    {
        public AddProcessConnectionRequest(string connectionId, int processId)
        {
            ConnectionId = connectionId;
            ProcessId = processId;
        }

        private string ConnectionId { get; }

        private int ProcessId { get; }

        public class Handler : IRequestHandler<AddProcessConnectionRequest>
        {
            private readonly IProcessManager _processManager;

            public Handler(IProcessManager processManager)
            {
                _processManager = processManager;
            }

            public Task<Unit> Handle(AddProcessConnectionRequest request, CancellationToken cancellationToken)
            {
                _processManager.AddProcess(request.ConnectionId, request.ProcessId);
                return Unit.Task;
            }
        }
    }
}