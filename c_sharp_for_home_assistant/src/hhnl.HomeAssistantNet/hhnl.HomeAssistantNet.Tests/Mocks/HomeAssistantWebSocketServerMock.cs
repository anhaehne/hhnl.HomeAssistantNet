using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.Mocks
{
    public class HomeAssistantWebSocketServerMock
    {
        public const string VALID_TOKEN = "TEST_TOKEN";

        private CancellationTokenSource? _cts;
        private WebSocket? _webSocket;
        private TaskCompletionSource? _authTcs;
        private HttpListener? _listener;
        private Task? _runTask;
        private Task? _sendTask;
        private Channel<byte[]>? _messagesToSend;

        public HomeAssistantWebSocketServerMock()
        {
        }

        public bool ClientConnected { get; private set; }

        public bool AuthHandShakeCompleted { get; private set;  }

        private WebSocket WebSocket => _webSocket ?? throw new InvalidOperationException("_webSocket not initialized.");
        private Channel<byte[]> MessagesToSend => _messagesToSend ?? throw new InvalidOperationException("_messagesToSend not initialized.");

        public async Task WaitForAuthHandshakeCompletedAsync(TimeSpan timeSpan)
        {
            var waitTask = _authTcs?.Task ?? Task.CompletedTask;

            await Task.WhenAny(waitTask, Task.Delay(timeSpan));

            if (!waitTask.IsCompleted)
                throw new TimeoutException("Waiting for the handshake to complete exceeded the timeout.");
        }

        public void Start(string listenerPrefix)
        {
            _runTask = Run();

            async Task Run()
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(listenerPrefix);
                _listener.Start();

                _cts = new CancellationTokenSource();
                _authTcs = new TaskCompletionSource();

                _messagesToSend = Channel.CreateBounded<byte[]>(10);
                _sendTask = SendLoopAsync();

                while (!_cts.IsCancellationRequested)
                {
                    HttpListenerContext listenerContext = await _listener.GetContextAsync();
                    if (listenerContext.Request.IsWebSocketRequest)
                    {
                        await ProcessClientAsync(listenerContext, _cts.Token);
                    }
                    else
                    {
                        listenerContext.Response.StatusCode = 400;
                        listenerContext.Response.Close();
                    }
                }

                await (_sendTask ?? Task.CompletedTask);
            }
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();
            try
            {
                await (_runTask ?? Task.CompletedTask);

            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private async Task SendLoopAsync()
        {
            while (!_cts?.IsCancellationRequested ?? false)
            {
                var bytes = await MessagesToSend.Reader.ReadAsync(_cts?.Token ?? default);

                await WebSocket.SendAsync(bytes,
                    WebSocketMessageType.Text,
                    true,
                    _cts?.Token ?? CancellationToken.None);
            }
        }

        private async Task ProcessClientAsync(HttpListenerContext listenerContext, CancellationToken cancellationToken)
        {

            var webSocketContext = await listenerContext.AcceptWebSocketAsync(null!);
            _webSocket = webSocketContext.WebSocket;
            ClientConnected = true;

            // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
            try
            {
                await SendMessageAsync(new WebSocketAuthRequired());
                while (WebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var message = await ReadNextAsync(cancellationToken);
                    await ProcessMessageAsync(message, cancellationToken);
                }
            }
            finally
            {
                WebSocket.Abort();
                WebSocket.Dispose();
                _webSocket = null;
                _listener?.Stop();
                ClientConnected = false;
            }
        }

        private async Task ProcessMessageAsync(JsonElement message, CancellationToken cancellationToken)
        {
            var baseMessage = (await message.ToObjectAsync<WebSocketApiMessageBase>()) ?? throw new InvalidOperationException("Received null message");

            switch (baseMessage.Type)
            {
                case "auth":
                    await HandleAuthAsync(message);
                    break;
            }
        }

        private async Task HandleAuthAsync(JsonElement message)
        {
            var authMessage = await message.ToObjectAsync<WebSocketAuth>();
            if (authMessage!.AccessToken == VALID_TOKEN)
            {
                await SendMessageAsync(new WebSocketAuthOk());
            }
            else
            {
                throw new InvalidOperationException("Received invalid token");
            }

            _authTcs?.TrySetResult();
            AuthHandShakeCompleted = true;
        }

        private async Task SendMessageAsync<T>(T message)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(message);
            await MessagesToSend.Writer.WriteAsync(bytes);
        }

        private async Task<JsonElement> ReadNextAsync(CancellationToken cancellationToken)
        {
            if (WebSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("Connection was closed.");

            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult? result;

            await using var ms = new MemoryStream();

            do
            {
                result = await _webSocket!.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    throw new InvalidOperationException("Connection was closed.");

                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            return await JsonSerializer.DeserializeAsync<JsonElement>(
                       ms,
                       cancellationToken: _cts?.Token ?? default);
        }
    }
}
