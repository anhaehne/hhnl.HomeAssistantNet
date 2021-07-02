using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Cronos;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationService
    {
        /// <summary>
        /// Enqueues a new automation and waits for it's start.
        /// </summary>
        /// <param name="automation">The automation to start.</param>
        /// <param name="changedEntity">The entity who's changes caused this automation to be enqueued.</param>
        Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity);

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
        private readonly ConcurrentDictionary<AutomationEntry, Lazy<AutomationRunner>> _runners =
            new();
        private CancellationTokenSource? _serviceCts;

        public AutomationService(
            IAutomationRegistry automationRegistry,
            IAutomationRunnerFactory automationRunnerFactory,
            ILogger<AutomationService> logger)
        {   
            _automationRegistry = automationRegistry;
            _automationRunnerFactory = automationRunnerFactory;
            _logger = logger;
        }

        public async Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity)
        {
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.EntityChanged, changedEntity, null);
        }

        public async Task EnqueueAutomationForManualStartAsync(AutomationEntry automation)
        {
            TaskCompletionSource tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.Manual, null, tcs);
            await tcs.Task;
        }

        public Task StopAutomationAsync(AutomationEntry automation)
        {
            var run = automation.LatestRun;

            if (run is null || run.State != AutomationRunInfo.RunState.Running)
                return Task.CompletedTask;

            run.CancellationTokenSource?.Cancel();
            return run.Task;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceCts = new();

            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            // Start all automations that are configure to run on startup
            foreach (var runOnStartAutomations in _automationRegistry.Automations.Values.Where(a => a.Info.RunOnStart))
            {
                await EnqueueAutomationRunAsync(runOnStartAutomations, AutomationRunInfo.StartReason.RunOnStart, null, null);
            }

            // Start schedules
            foreach (var scheduledAutomation in _automationRegistry.Automations.Values.Where(a => a.Info.Schedules.Any()))
            {
                ScheduleNextRun(scheduledAutomation);
            }
        }

        private void ScheduleNextRun(AutomationEntry entry)
        {
            var nextOccurence = GetNextOccurance(entry);

            if(!nextOccurence.HasValue)
            {
                _logger.LogWarning($"Automation {entry.Info.Name} has no next scheduled date. Cron expressions {string.Join(", ", entry.Info.Schedules)}");
                return;
            }

            var runIn = nextOccurence.Value - DateTime.Now;

            var t = new System.Timers.Timer(runIn.TotalMilliseconds);
            t.Elapsed += ScheduleRun;
            t.Start();

            async void ScheduleRun(object sender, ElapsedEventArgs e)
            {
                try
                {
                    t.Stop();
                    t.Elapsed -= ScheduleRun;

                    if ((_serviceCts?.Token ?? default).IsCancellationRequested)
                        return;

                    await EnqueueAutomationRunAsync(entry, AutomationRunInfo.StartReason.Schedule, null, null);

                    ScheduleNextRun(entry);
                }
                catch(Exception ex)
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
            var nextOccurences = entry.Info.Schedules.Select(exp =>
            {
                if (TryParseExpression(exp, out var cronExpression))
                    return (IsValid: true, CronExpression: (CronExpression?)cronExpression);

                return (false, null);
            }).Where(x => x.IsValid)
            .Select(x => x.CronExpression!.GetNextOccurrence(DateTime.UtcNow))
            .Where(x => x.HasValue)
            .Select(x => x!.Value.ToLocalTime())
            .ToList();

            if (!nextOccurences.Any())
                return null;

            return nextOccurences.Min();

            bool TryParseExpression(string expression, [NotNullWhen(true)]out CronExpression? cronExpression)
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

            var runners = _runners.Values.Where(v => v.IsValueCreated).Select(v => v.Value).ToList();

            _logger.LogInformation("Stopping running automations ...");
            await Task.WhenAll(runners.Select(r => r.StopAsync()));
            _logger.LogInformation("Automations stopped");
        }

        private Task EnqueueAutomationRunAsync(
            AutomationEntry entry,
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs)
        {
            // Create entity snapshot
            if (entry.Info.SnapshotEntities.Any())
            {
                var snapshotEntities = entry.Info.SnapshotEntities.Select(e => e.ToString());
                _logger.LogDebug($"Creating snapshot of entities {string.Join(", ", snapshotEntities)}.");

                // TODO: Create snapshot and IEntitySnapshotProvider.
            }

            _logger.LogDebug($"Enqueueing automation run '{entry.Info.Name}'. Reason: '{reason}'");

            var runner = _runners.GetOrAdd(entry,
                e => new Lazy<AutomationRunner>(() =>
                {
                    var runner = _automationRunnerFactory.CreateRunnerFor(e);
                    runner.Start();
                    return runner;
                }));

            return runner.Value.EnqueueAsync(reason, changedEntity, startTcs);
        }
    }
}