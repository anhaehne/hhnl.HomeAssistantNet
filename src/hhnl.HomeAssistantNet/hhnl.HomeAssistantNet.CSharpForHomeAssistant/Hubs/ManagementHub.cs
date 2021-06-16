using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs
{
    public class ManagementHub : Hub<IManagementClient>
    {
        private readonly IMediator _mediator;
        private readonly INotificationQueue _notificationQueue;
        private static int _clientCount = 0;

        public ManagementHub(IMediator mediator, INotificationQueue notificationQueue)
        {
            _mediator = mediator;
            _notificationQueue = notificationQueue;
        }

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _clientCount);
            await _notificationQueue.Enqueue(new HubConnectionAddedNotification(Context.ConnectionId));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var wasLastConnection = Interlocked.Decrement(ref _clientCount) == 0;
            
            await _notificationQueue.Enqueue(new HubConnectionClosedNotification(Context.ConnectionId));
            
            if(wasLastConnection)
                await _notificationQueue.Enqueue(NoConnectionNotification.Instance);
        }

        public Task AutomationStopped(long messageId, ManagementAutomationInfo? info)
        {
            return _mediator.Send(new SetHubCallResultRequest(info, messageId, Context.ConnectionId));
        }

        public Task AutomationStarted(long messageId, ManagementAutomationInfo? info)
        {
            return _mediator.Send(new SetHubCallResultRequest(info, messageId, Context.ConnectionId));
        }

        public Task AutomationsGot(long messageId, IReadOnlyCollection<ManagementAutomationInfo> infos)
        {
            return _mediator.Send(new SetHubCallResultRequest(infos, messageId, Context.ConnectionId));
        }
        
        public Task ProcessIdGot(long messageId, int processId)
        {
            return _mediator.Send(new SetHubCallResultRequest(processId, messageId, Context.ConnectionId));
        }
    }
}