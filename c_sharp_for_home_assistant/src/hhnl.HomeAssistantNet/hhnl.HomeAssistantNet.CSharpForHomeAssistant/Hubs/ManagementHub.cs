using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs
{
    public class ManagementHub : Hub<IManagementClient>
    {
        private static int _clientCount;
        private readonly IMediator _mediator;
        private readonly INotificationQueue _notificationQueue;
        private readonly ILogger<ManagementHub> _logger;

        public ManagementHub(IMediator mediator, INotificationQueue notificationQueue, ILogger<ManagementHub> logger)
        {
            _mediator = mediator;
            _notificationQueue = notificationQueue;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _clientCount);

            var remoteAddress = Context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress;

            if (remoteAddress is null)
            {
                _logger.LogError($"Could not determine remote address for connection {Context.ConnectionId}. Aborting connection.");
                Context.Abort();
                return;
            }
            
            await _notificationQueue.Enqueue(new HubConnectionAddedNotification(Context.ConnectionId, !IPAddress.IsLoopback(remoteAddress)));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var wasLastConnection = Interlocked.Decrement(ref _clientCount) == 0;

            await _notificationQueue.Enqueue(new HubConnectionClosedNotification(Context.ConnectionId));

            if (wasLastConnection)
                await _notificationQueue.Enqueue(NoConnectionNotification.Instance);
        }

        public Task AutomationStopped(long messageId, AutomationInfoDto? info)
        {
            return _mediator.Send(new SetHubCallResultRequest(info, messageId, Context.ConnectionId));
        }

        public Task AutomationStarted(long messageId, AutomationInfoDto? info)
        {
            return _mediator.Send(new SetHubCallResultRequest(info, messageId, Context.ConnectionId));
        }

        public Task AutomationsGot(long messageId, IReadOnlyCollection<AutomationInfoDto> infos)
        {
            return _mediator.Send(new SetHubCallResultRequest(infos, messageId, Context.ConnectionId));
        }

        public Task ProcessIdGot(long messageId, int processId)
        {
            return _mediator.Send(new SetHubCallResultRequest(processId, messageId, Context.ConnectionId));
        }
    }
}