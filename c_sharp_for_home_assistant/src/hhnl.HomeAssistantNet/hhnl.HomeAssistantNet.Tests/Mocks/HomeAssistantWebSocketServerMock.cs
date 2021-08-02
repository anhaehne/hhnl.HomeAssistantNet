using hhnl.HomeAssistantNet.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Tests.Mocks
{
    public class HomeAssistantWebSocketServerMock
    {
        public const string VALID_TOKEN = "TEST_TOKEN";

        private readonly List<Entity> _entities;
        private CancellationTokenSource? _cts;


        public HomeAssistantWebSocketServerMock(List<Entity>? entities = null)
        {
            _entities = entities ?? new List<Entity>();
        }

        public bool ClientConnected { get; private set; }

        public bool HandShakeCompleted { get; private set;  }

        public async void Start(string listenerPrefix)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();

            _cts = new CancellationTokenSource();

            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
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
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task ProcessClientAsync(HttpListenerContext listenerContext, CancellationToken cancellationToken)
        {

            var webSocketContext = await listenerContext.AcceptWebSocketAsync(null!);
            WebSocket webSocket = webSocketContext.WebSocket;
            ClientConnected = true;

            // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {

                }
            }
            finally
            {
                webSocket.Abort();
                webSocket.Dispose();
                ClientConnected = false;
            }
        }
    }
}
