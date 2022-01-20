using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public class HomeAssistantClient : IHostedService, IDisposable, IHomeAssistantClient
    {
        private readonly ConcurrentDictionary<long, TaskCompletionSource<WebsocketApiMessage>> _callResults = new();
        private readonly SemaphoreSlim _enqueueLock = new(1);
        private readonly IOptions<HomeAssistantConfig> _haConfig;
        private readonly ILogger<HomeAssistantClient> _logger;
        private readonly IMediator _mediator;
        private readonly bool _publishEventNotification;
        private CancellationTokenSource? _cancellationTokenSource;
        private long _id;
        private Channel<byte[]>? _messagesToSend;
        private Task? _receiveTask;
        private Task? _sendTask;
        private ClientWebSocket? _webSocket;

        public HomeAssistantClient(
            ILogger<HomeAssistantClient> logger,
            IOptions<HomeAssistantConfig> haConfig,
            IMediator mediator,
            IAutomationRegistry automationRegistry)
        {
            _logger = logger;
            _haConfig = haConfig;
            _mediator = mediator;
            _publishEventNotification = automationRegistry.HasAutomationsTrackingAnyEvents;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
        }

        public async Task<JsonElement> FetchStatesAsync(CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync(id => new
            {
                id,
                type = "get_states"
            },
                cancellationToken);

            return response.Result;
        }

        public async Task<JsonElement> CallServiceAsync(
            string domain,
            string service,
            dynamic? serviceData = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default && AutomationRunContext.Current is not null)
                cancellationToken = AutomationRunContext.Current.CancellationToken;


            var response = await SendRequestAsync(id => new
            {
                id,
                type = "call_service",
                domain,
                service,
                service_data = serviceData
            },
                cancellationToken);

            var currentRun = AutomationRunContext.Current?.CurrentRun;
            if (currentRun is not null)
                currentRun.ServiceCallCount++;

            return response.Result;
        }

        public async Task<JsonElement> CallServiceAsync(
            string domain,
            string service,
            string targetId,
            dynamic? serviceData = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken == default && AutomationRunContext.Current is not null)
                cancellationToken = AutomationRunContext.Current.CancellationToken;

            var response = await SendRequestAsync(id => new
            {
                id,
                type = "call_service",
                domain,
                service,
                target = new
                {
                    entity_id = targetId,
                },
                service_data = serviceData ?? new { }
            },
            cancellationToken);

            var currentRun = AutomationRunContext.Current?.CurrentRun;
            if (currentRun is not null)
                currentRun.ServiceCallCount++;

            return response.Result;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Policy.Handle<Exception>(e => e is not TaskCanceledException && e is not OperationCanceledException)
                .WaitAndRetryForeverAsync(
                (_, _) => TimeSpan.FromSeconds(5),
                (ex, retry, _, _) =>
                {
                    _logger.LogError(ex, $"Unable to connect to home assistant. Retry: {retry}");
                })
            .ExecuteAsync(ct => ConnectAsync(ct), cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping HomeAssistantClient ...");

            _webSocket?.Abort();
            _webSocket?.Dispose();

            _cancellationTokenSource?.Cancel();

            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            if (_sendTask != null)
            {
                try
                {
                    await _sendTask;
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            _logger.LogDebug("HomeAssistantClient stopped");

            _webSocket = null;
            _messagesToSend = null;
            _receiveTask = null;
            _sendTask = null;
            _cancellationTokenSource = null;
        }

        private async Task ReceiveLoopAsync()
        {
            while (!_cancellationTokenSource?.IsCancellationRequested ?? false)
            {
                try
                {
                    var received = await ReadNextEventAsync();

                    if (received == null)
                    {
                        // Web socket connection disconnected. Reconnect and try again.
                        Initialization.HomeAssistantDisconnected();
                        StartReconnect();
                        continue;
                    }

                    var incremented = received.Id + 1;
                    Interlocked.CompareExchange(ref _id, incremented, received.Id);

                    var receiveTask = HandleMessageAsync(received);

                    await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(10)));

                    if (!receiveTask.IsCompleted)
                        throw new TimeoutException("Receive timeout exceeded.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error occured while handling message from home assistant.");
                }
            }

            async void StartReconnect()
            {
                await Policy.Handle<Exception>().WaitAndRetryForeverAsync(
                    (_, _) => TimeSpan.FromSeconds(5),
                    (ex, retry, _, _) =>
                    {
                        _logger.LogError(ex, $"Unable to connect to home assistant. Retry: {retry}");
                    })
                    .ExecuteAsync(() => ConnectAsync(default));
            }
        }

        private async Task SendLoopAsync()
        {
            while (!_cancellationTokenSource?.IsCancellationRequested ?? false)
            {
                var bytes = await _messagesToSend!.Reader.ReadAsync(_cancellationTokenSource?.Token ?? default);

                await _webSocket!.SendAsync(bytes,
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
        }

        private async Task HandleMessageAsync(WebsocketApiMessage apiMessage)
        {
            switch (apiMessage.Type)
            {
                case "auth_required":

                    _logger.LogDebug("Got auth_required; sending token.");

                    await EnqueueMessageAsync(_ => new WebSocketAuth(_haConfig.Value.SUPERVISOR_TOKEN));

                    break;
                case "auth_ok":

                    _logger.LogDebug("Got auth_ok; Init complete.");

                    Initialization.HomeAssistantConnected();

                    await _mediator.Publish(HomeAssistantClientConnectedNotification.Instance);

                    await EnqueueMessageAsync(i =>
                        new
                        {
                            id = i,
                            type = "subscribe_events"
                        });

                    break;
                case "result":


                    if (_callResults.TryGetValue(apiMessage.Id, out var tsc))
                        tsc.SetResult(apiMessage);

                    break;
                case "event":
                    var apiEvent = await apiMessage.Event.ToObjectAsync<Events.Current>();

                    if (apiEvent is null)
                        return;

                    await HandleEventAsync(apiEvent);

                    break;
            }
        }

        private async Task HandleEventAsync(Events.Current apiEvent)
        {
            switch (apiEvent.EventType)
            {
                case "state_changed":
                    var eventData = await apiEvent.Data.ToObjectAsync<StateChangedNotification>();

                    if (eventData is null)
                        return;

                    eventData = eventData with { SourceEvent = apiEvent };

                    await _mediator.Publish(eventData);
                    break;
            }

            // We only publish this event if anyone is listening.
            if (_publishEventNotification)
                await _mediator.Publish(new EventFiredNotification(apiEvent));
        }

        private async Task<WebsocketApiMessage?> ReadNextEventAsync()
        {
            if (_webSocket!.State != WebSocketState.Open)
                return null;

            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult? result;

            await using var ms = new MemoryStream();

            do
            {
                try
                {
                    result = await _webSocket!.ReceiveAsync(buffer, _cancellationTokenSource!.Token);
                }
                catch (WebSocketException)
                {
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                using var sr = new StreamReader(ms, Encoding.UTF8, false, 1024, true);
                var json = await sr.ReadToEndAsync();

                _logger.LogTrace($"Received message: \r\n {json}");

                ms.Seek(0, SeekOrigin.Begin);
            }

            return await JsonSerializer.DeserializeAsync<WebsocketApiMessage>(
                       ms,
                       cancellationToken: _cancellationTokenSource.Token) ??
                   throw new InvalidOperationException("Message expected but got null.");
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            // Close previous
            if (_cancellationTokenSource is not null)
                await StopAsync(cancellationToken);

            _cancellationTokenSource = new CancellationTokenSource();
            _messagesToSend = Channel.CreateBounded<byte[]>(10);
            _id = 1;
            _webSocket = new ClientWebSocket();

            var baseUri = new Uri(_haConfig.Value.HOME_ASSISTANT_API);
            var completeUri = new Uri(baseUri, "api/websocket");

            var uriBuilder = new UriBuilder(completeUri);
            uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";

            _logger.LogInformation(
                $"Starting home assistant client. Url '{uriBuilder}' Token '{_haConfig.Value.SUPERVISOR_TOKEN.Substring(0, 10)}...'");

            await _webSocket.ConnectAsync(uriBuilder.Uri, cancellationToken);

            _logger.LogInformation("Connected to home assistant websocket api.");

            _receiveTask = Task.Run(ReceiveLoopAsync).ContinueWith(task =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _logger.LogError("The receive task has completed even though the runner hasn't been stopped.");
            });

            _sendTask = Task.Run(SendLoopAsync).ContinueWith(task =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _logger.LogError("The send task has completed even though the runner hasn't been stopped.");
            });
        }

        private async Task<WebsocketApiMessage> SendRequestAsync<T>(
            Func<long, T> requestFactory,
            CancellationToken cancellationToken)
        {
            // Wait for init
            await Initialization.WaitForHomeAssistantConnectionAsync();

            TaskCompletionSource<WebsocketApiMessage>? tcs = null;

            var id = await EnqueueMessageAsync(innerId =>
            {
                var request = requestFactory(innerId);
                tcs = _callResults.GetOrAdd(innerId, i => new TaskCompletionSource<WebsocketApiMessage>(cancellationToken));
                return request;
            });

            Debug.Assert(tcs is not null);

            try
            {
                var result = await tcs.Task;

                if (result.Success == false)
                {
                    var ex = new HomeAssistantCallFailedException(result.Error!.Code!, result.Error.Message!);
                    _logger.LogError(ex, "Home assistant api doesn't indicate success.");
                    throw ex;
                }

                return result;
            }
            finally
            {
                _callResults.TryRemove(id, out _);
            }
        }


        private async Task<long> EnqueueMessageAsync<T>(Func<long, T> createMessageAsync)
        {
            await _enqueueLock.WaitAsync();

            try
            {
                var messageId = Interlocked.Increment(ref _id);
                var message = createMessageAsync(messageId);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
                await _messagesToSend!.Writer.WriteAsync(bytes);
                return messageId;
            }
            finally
            {
                _enqueueLock.Release();
            }
        }

        public class HomeAssistantCallFailedException : Exception
        {
            public HomeAssistantCallFailedException(string code, string message)
                : base(message)
            {
                Code = code;
            }

            public string Code { get; }
        }

        public class EventFiredNotification : INotification
        {
            public EventFiredNotification(Events.Current @event)
            {
                Event = @event;
            }

            public Events.Current Event { get; set; }
        }
    }
}