using hhnl.HomeAssistantNet.Automations.Supervisor;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class AutomationLogger : ILogger
    {
        private readonly string _name;
        private static int _notificationId;
        private static ConcurrentDictionary<Guid, bool> _subscribedRuns = new();

        public AutomationLogger(string name)
        {
            _name = name;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => AutomationRunContext.Current is not null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var runContext = AutomationRunContext.Current;

            if (runContext is null)
                return;

            var runTime = DateTimeOffset.Now - runContext.CurrentRun.Started;

            var message = $"[{logLevel,-12};{_name};{runTime}]{formatter(state, exception)}";

            var messageDto = new LogMessageDto(runContext.CurrentRun.Id, message, (int)logLevel, Interlocked.Increment(ref _notificationId));

            lock(runContext.CurrentRun)
                runContext.CurrentRun.Log.Add(messageDto);

            if (_subscribedRuns.ContainsKey(runContext.CurrentRun.Id))
                runContext.ServiceProvider.GetRequiredService<SupervisorClient>().OnNewLogMessage(messageDto);
        }

        public static void RegisterRun(Guid runId) => _subscribedRuns.TryAdd(runId, false);

        public static void UnregisterRun(Guid runId) => _subscribedRuns.TryRemove(runId, out _);

        public class Provider : ILoggerProvider
        {
            private readonly ConcurrentDictionary<string, AutomationLogger> _loggers = new();

            public ILogger CreateLogger(string categoryName)
                => _loggers.GetOrAdd(categoryName, name => new AutomationLogger(name));

            public void Dispose()
            {
                _loggers.Clear();
            }
        }
    }
}
