using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests
{
    public class SetHubCallResultRequest : IRequest
    {
        public SetHubCallResultRequest(object? result, long messageId, string connectionId)
        {
            Result = result;
            MessageId = messageId;
            ConnectionId = connectionId;
        }

        private object? Result { get; }

        private long MessageId { get; }

        private string ConnectionId { get; }

        public class Handler : IRequestHandler<SetHubCallResultRequest>
        {
            private readonly IManagementHubCallService _managementHubCallService;

            public Handler(IManagementHubCallService managementHubCallService)
            {
                _managementHubCallService = managementHubCallService;
            }

            public Task<Unit> Handle(SetHubCallResultRequest request, CancellationToken cancellationToken)
            {
                _managementHubCallService.SetResult(request.ConnectionId, request.MessageId, request.Result);
                return Unit.Task;
            }
        }
    }
}