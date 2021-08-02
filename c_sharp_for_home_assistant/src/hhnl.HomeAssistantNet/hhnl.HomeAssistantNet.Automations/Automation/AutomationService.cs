using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Automations.Triggers;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static hhnl.HomeAssistantNet.Shared.Automation.AutomationRunInfo;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationService
    {
        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        Task EnqueueAutomationAsync(AutomationEntry automation, StartReason reason, string? reasonMessage = null, Events.Current? currentEvent = null, bool waitForStart = false);

        Task StopAutomationRunAsync(AutomationEntry automation, AutomationRunInfo run);
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

        public async Task EnqueueAutomationAsync(AutomationEntry automation, StartReason reason, string? reasonMessage, Events.Current? currentEvent, bool waitForStart = false)
        {
            TaskCompletionSource? tcs = null;

            if (waitForStart)
                tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await EnqueueAutomationRunAsync(automation, reason, reasonMessage, tcs, currentEvent ?? Events.Empty);

            if (tcs is not null)
                await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceCts = new();

            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            _logger.LogInformation("Registering all triggers ...");
            // Register all automation triggers
            foreach (var (automation, trigger) in GetTriggerAttributes())
            {
                await trigger.RegisterTriggerAsync(automation, this, _serviceProvider);
            }
            _logger.LogInformation("AutomationService started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _serviceCts?.Cancel();


            _logger.LogInformation("Unregistering all triggers ...");
            // Unregister all automation triggers
            foreach (var (automation, trigger) in GetTriggerAttributes())
            {
                await trigger.UnregisterTriggerAsync();
            }

            var runners = _runners.Values.Where(v => v.IsValueCreated).Select(v => v.Value).ToList();

            _logger.LogInformation("Stopping running automations ...");
            await Task.WhenAll(runners.Select(r => r.StopAsync()));
            _logger.LogInformation("Automations stopped");
        }

        public Task StopAutomationRunAsync(AutomationEntry automation, AutomationRunInfo run)
        {
            if (!_runners.TryGetValue(automation, out var runner))
                throw new InvalidOperationException($"Can't find runner for automation {automation.Info.Name}.");

            return Task.WhenAny(runner.Value.StopRunAsync(run), Task.Delay(TimeSpan.FromSeconds(2)));
        }

        private IEnumerable<(AutomationEntry Automation, AutomationTriggerAttributeBase Trigger)> GetTriggerAttributes()
        {
            foreach (var automation in _automationRegistry.Automations.Values)
            {
                var triggerAttributes = automation.Info.Method.GetCustomAttributes(true).OfType<AutomationTriggerAttributeBase>();
                foreach (var trigger in triggerAttributes)
                {
                    yield return (automation, trigger);
                }
            }
        }

        private Task EnqueueAutomationRunAsync(
            AutomationEntry entry,
            StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            Events.Current currentEvent)
        {
            Dictionary<Type, object>? snapshot = null;

            // Create entity snapshot
            if (entry.Info.SnapshotEntities.Any())
            {
                var snapshotEntities = entry.Info.SnapshotEntities.Select(e => e.ToString());
                _logger.LogDebug($"Creating snapshot of entities {string.Join(", ", snapshotEntities)}.");

                snapshot = entry.Info.SnapshotEntities.ToDictionary(x => x, x => CreateEntitySnapshot(x));
            }

            _logger.LogDebug($"Enqueueing automation run '{entry.Info.Name}'. Reason: '{reason}'");

            Lazy<AutomationRunner>? runner = _runners.GetOrAdd(entry,
                e => new Lazy<AutomationRunner>(() =>
                {
                    var runner = _automationRunnerFactory.CreateRunnerFor(e);
                    runner.Start();
                    return runner;
                }));

            return runner.Value.EnqueueAsync(reason, reasonMessage, startTcs, snapshot ?? _emptySnapshot);

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