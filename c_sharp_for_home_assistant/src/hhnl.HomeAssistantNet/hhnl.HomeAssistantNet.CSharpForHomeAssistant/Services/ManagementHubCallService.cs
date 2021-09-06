using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Polly;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IManagementHubCallService
    {
        SupervisorConnectionInfo? DefaultConnection { get; set; }

        Task<T?> CallService<T>(
            Func<long, IManagementClient, Task> call,
            TimeSpan? timeout = null,
            string? connectionId = null);

        void SetResult(string connectionId, long messageId, object? result);
        void RemoveConnection(string connectionId);
    }

    public class ManagementHubCallService : IManagementHubCallService
    {
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
        private readonly IOptions<SupervisorConfig> _config;
        private readonly Policy _defaultPolicy = Policy.Handle<TaskCanceledException>().Retry(3);
        private readonly IHubContext<ManagementHub, IManagementClient> _hubContext;
        private long _messageIdCounter;

        public ManagementHubCallService(
            IHubContext<ManagementHub, IManagementClient> hubContext,
            IOptions<SupervisorConfig> config)
        {
            _hubContext = hubContext;
            _config = config;
        }

        public async Task<T?> CallService<T>(
            Func<long, IManagementClient, Task> call,
            TimeSpan? timeout = null,
            string? connectionId = null)
        {
            // Retry 3 times to make sure we get results even when the clients are switching.
            return await _defaultPolicy.Execute(async () =>
            {
                if (connectionId is null)
                {
                    if (DefaultConnection is null)
                        throw new NoAutomationHostConnectedException();

                    connectionId = DefaultConnection.Id;
                }

                var connection = _hubContext.Clients.Client(connectionId);

                var messageId = Interlocked.Increment(ref _messageIdCounter);
                var completionSource = new TaskCompletionSource<object?>();

                var connectionInfo = _clients.GetOrAdd(connectionId, s => new ClientInfo(s));

                connectionInfo.MessageCompletionSources.TryAdd(messageId, completionSource);
                await call(messageId, connection);

                using var cancellationTokenSource =
                    new CancellationTokenSource(timeout ?? _config.Value.DefaultClientCallTimeout);

                await using var reg = cancellationTokenSource.Token.Register(() =>
                {
                    completionSource.TrySetCanceled();
                    connectionInfo.MessageCompletionSources.TryRemove(messageId, out _);
                });

                return (T?)await completionSource.Task;
            });
        }

        public SupervisorConnectionInfo? DefaultConnection { get; set; }

        public void SetResult(string connectionId, long messageId, object? result)
        {
            if (!_clients.TryGetValue(connectionId, out var connection))
                return;

            if (!connection.MessageCompletionSources.TryGetValue(messageId, out var tcs))
                return;

            tcs.TrySetResult(result);
        }

        public void RemoveConnection(string connectionId)
        {
            if (!_clients.TryGetValue(connectionId, out var connection))
                return;

            foreach (var messageCompletionSource in connection.MessageCompletionSources)
            {
                messageCompletionSource.Value.TrySetCanceled();
            }
        }

        private class ClientInfo
        {
            private long _messageIdCounter;

            public ClientInfo(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public ConcurrentDictionary<long, TaskCompletionSource<object?>> MessageCompletionSources { get; } = new();

            public long GetNextMessageId()
            {
                return Interlocked.Increment(ref _messageIdCounter);
            }
        }
    }

    public class NoAutomationHostConnectedException : Exception
    {
    }
}