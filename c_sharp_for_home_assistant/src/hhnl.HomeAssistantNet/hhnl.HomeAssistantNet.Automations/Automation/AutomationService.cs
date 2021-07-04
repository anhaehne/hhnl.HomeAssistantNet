using Cronos;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationService
    {
        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        /// <param name="changedEntity">The entity who's changes caused this automation to be enqueued.</param>
        /// <param name="currentEvent">The current event.</param>
        Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity, Events.Current currentEvent);

        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        /// <param name="currentEvent">The current event.</param>
        Task EnqueueAutomationForEventFiredAsync(AutomationEntry automation, Events.Current currentEvent);

        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        Task EnqueueAutomationForManualStartAsync(AutomationEntry automation);

        Task StopAutomationAsync(AutomationEntry automation);
    }

    public class AutomationService : IHostedService, IAutomationService
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IAutomationRunnerFactory _automationRunnerFactory;
        private readonly ILogger<AutomationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<AutomationEntry, Lazy<AutomationRunner>> _runners =
            new();
        private static readonly Dictionary<Type, object> _emptySnapshot = new();
        private CancellationTokenSource? _serviceCts;

        public AutomationService(
            IAutomationRegistry automationRegistry,
            IAutomationRunnerFactory automationRunnerFactory,
            ILogger<AutomationService> logger,
            IServiceProvider serviceProvider)
        {
            _automationRegistry = automationRegistry;
            _automationRunnerFactory = automationRunnerFactory;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity, Events.Current currentEvent)
        {
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.EntityChanged, changedEntity, null, currentEvent);
        }

        public async Task EnqueueAutomationForEventFiredAsync(AutomationEntry automation, Events.Current currentEvent)
        {
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.EventFired, null, null, currentEvent);
        }

        public async Task EnqueueAutomationForManualStartAsync(AutomationEntry automation)
        {
            TaskCompletionSource tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.Manual, null, tcs, Events.Empty);
            await tcs.Task;
        }

        public Task StopAutomationAsync(AutomationEntry automation)
        {
            AutomationRunInfo? run = automation.LatestRun;

            if (run is null || run.State != AutomationRunInfo.RunState.Running)
            {
                return Task.CompletedTask;
            }

            run.CancellationTokenSource?.Cancel();
            return run.Task;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceCts = new();

            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            // Start all automations that are configure to run on startup
            foreach (AutomationEntry? runOnStartAutomations in _automationRegistry.Automations.Values.Where(a => a.Info.RunOnStart))
            {
                await EnqueueAutomationRunAsync(runOnStartAutomations, AutomationRunInfo.StartReason.RunOnStart, null, null, Events.Empty);
            }

            // Start schedules
            foreach (AutomationEntry? scheduledAutomation in _automationRegistry.Automations.Values.Where(a => a.Info.Schedules.Any()))
            {
                ScheduleNextRun(scheduledAutomation);
            }
        }

        private void ScheduleNextRun(AutomationEntry entry)
        {
            DateTime? nextOccurence = GetNextOccurance(entry);

            if (!nextOccurence.HasValue)
            {
                _logger.LogWarning($"Automation {entry.Info.Name} has no next scheduled date. Cron expressions {string.Join(", ", entry.Info.Schedules)}");
                return;
            }

            TimeSpan runIn = nextOccurence.Value - DateTime.Now;

            System.Timers.Timer? t = new System.Timers.Timer(runIn.TotalMilliseconds);
            t.Elapsed += ScheduleRun;
            t.Start();

            async void ScheduleRun(object sender, ElapsedEventArgs e)
            {
                try
                {
                    t.Stop();
                    t.Elapsed -= ScheduleRun;

                    if ((_serviceCts?.Token ?? default).IsCancellationRequested)
                    {
                        return;
                    }

                    await EnqueueAutomationRunAsync(entry, AutomationRunInfo.StartReason.Schedule, null, null, Events.Empty);

                    ScheduleNextRun(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occured while enqueueing the scheduled next automation run of '{entry.Info.Name}'.");
                }
                finally
                {
                    t.Dispose();
                }
            }
        }

        private DateTime? GetNextOccurance(AutomationEntry entry)
        {
            List<DateTime>? nextOccurences = entry.Info.Schedules.Select(exp =>
            {
                if (TryParseExpression(exp, out CronExpression? cronExpression))
                {
                    return (IsValid: true, CronExpression: (CronExpression?)cronExpression);
                }

                return (false, null);
            }).Where(x => x.IsValid)
            .Select(x => x.CronExpression!.GetNextOccurrence(DateTime.UtcNow))
            .Where(x => x.HasValue)
            .Select(x => x!.Value.ToLocalTime())
            .ToList();

            if (!nextOccurences.Any())
            {
                return null;
            }

            return nextOccurences.Min();

            bool TryParseExpression(string expression, [NotNullWhen(true)] out CronExpression? cronExpression)
            {
                try
                {
                    cronExpression = CronExpression.Parse(expression, CronFormat.IncludeSeconds);
                    return true;
                }
                catch (CronFormatException)
                {
                    cronExpression = null;
                    return false;
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _serviceCts?.Cancel();

            List<AutomationRunner>? runners = _runners.Values.Where(v => v.IsValueCreated).Select(v => v.Value).ToList();

            _logger.LogInformation("Stopping running automations ...");
            await Task.WhenAll(runners.Select(r => r.StopAsync()));
            _logger.LogInformation("Automations stopped");
        }

        private Task EnqueueAutomationRunAsync(
            AutomationEntry entry,
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs,
            Events.Current currentEvent)
        {
            Dictionary<Type, object>? snapshot = null;

            // Create entity snapshot
            if (entry.Info.SnapshotEntities.Any())
            {
                IEnumerable<string>? snapshotEntities = entry.Info.SnapshotEntities.Select(e => e.ToString());
                _logger.LogDebug($"Creating snapshot of entities {string.Join(", ", snapshotEntities)}.");

                snapshot = entry.Info.SnapshotEntities.ToDictionary(x => x, x => CreateEntitySnapshot(x));
            }

            _logger.LogDebug($"Enqueueing automation run '{entry.Info.Name}'. Reason: '{reason}'");

            Lazy<AutomationRunner>? runner = _runners.GetOrAdd(entry,
                e => new Lazy<AutomationRunner>(() =>
                {
                    AutomationRunner? runner = _automationRunnerFactory.CreateRunnerFor(e);
                    runner.Start();
                    return runner;
                }));

            return runner.Value.EnqueueAsync(reason, changedEntity, startTcs, snapshot ?? _emptySnapshot);

            object CreateEntitySnapshot(Type entityType)
            {
                if (entityType == typeof(Events.Current))
                    return currentEvent;

                if (entityType == typeof(Events.Any))
                    return new Events.Any(currentEvent);

                if (!entityType.IsAssignableTo(typeof(Entity)))
                    throw new InvalidOperationException($"Type {entityType} is not an entity");

                var snapshotEntity = (Entity)ActivatorUtilities.CreateInstance(_serviceProvider, entityType);
                snapshotEntity.CurrentState = ((Entity)_serviceProvider.GetRequiredService(entityType)).CurrentState;
                return snapshotEntity;
            }
        }
    }
}