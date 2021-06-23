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
            private readonly IProcessManager _processManager;

            public Handler(IProcessManager processManager, IHubCallService callService, IOptions<SupervisorConfig> config)
            {
                _processManager = processManager;
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

                if (cancellationToken.IsCancellationRequested)
                    return Unit.Value;

                using var cts = new CancellationTokenSource(_config.Value.DefaultProcessExitTimeout);
                using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

                // If this is a local process we make sure the process gets closed.
                if (!_processManager.TryGetProcess(request.ConnectionId, out var processInfo))
                    return Unit.Value;

                if (processInfo.NativeProcess is null)
                    return Unit.Value;

                if (processInfo.NativeProcess.HasExited)
                    return Unit.Value;
                
                try
                {
                    await processInfo.NativeProcess.WaitForExitAsync(combinedSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // If we exceeded the process exit timeout we force close the process.
                    if (!cancellationToken.IsCancellationRequested)
                        processInfo.NativeProcess.Close();
                }

                // Make sure the process has exited.
                if (!cancellationToken.IsCancellationRequested && !processInfo.NativeProcess.HasExited)
                    throw new UnableToStopProcessesException(processInfo.NativeProcess.Id);

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