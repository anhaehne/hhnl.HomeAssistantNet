using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface INotificationQueue
    {
        ValueTask Enqueue(INotification notification);
    }

    public class NotificationQueue : IHostedService, INotificationQueue
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly IEnumerable<INotification> _initNotifications;
        private readonly ILogger<NotificationQueue> _logger;
        private readonly Channel<INotification> _notificationChannel = Channel.CreateUnbounded<INotification>();
        private readonly IServiceScopeFactory _scopeFactory;
        private Task _runTask = Task.CompletedTask;

        public NotificationQueue(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationQueue> logger,
            IEnumerable<INotification> initNotifications)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _initNotifications = initNotifications;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var initNotification in _initNotifications)
            {
                await _notificationChannel.Writer.WriteAsync(initNotification, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            _runTask = RunAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            try
            {
                await _runTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        public ValueTask Enqueue(INotification notification)
        {
            return _notificationChannel.Writer.WriteAsync(notification);
        }

        private async Task RunAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                var notification = await _notificationChannel.Reader.ReadAsync(_cts.Token);
                using var scope = _scopeFactory.CreateScope();

                try
                {
                    await scope.ServiceProvider.GetRequiredService<IMediator>().Publish(notification, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Ignored
                }
                catch (Exception e) when (e is not TaskCanceledException)
                {
                    _logger.LogError(e, $"An exception occured while processing notification {notification}.");
                    throw;
                }
            }
        }
    }
}