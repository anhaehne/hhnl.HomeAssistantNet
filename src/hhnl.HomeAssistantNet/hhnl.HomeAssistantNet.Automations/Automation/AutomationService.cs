using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.Hosting;

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

        private readonly ConcurrentDictionary<AutomationEntry, Lazy<AutomationRunner>> _runners =
            new();

        public AutomationService(
            IAutomationRegistry automationRegistry,
            IAutomationRunnerFactory automationRunnerFactory)
        {
            _automationRegistry = automationRegistry;
            _automationRunnerFactory = automationRunnerFactory;
        }

        public async Task EnqueueAutomationForEntityChangeAsync(AutomationEntry automation, string changedEntity)
        {
            TaskCompletionSource<bool> tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.DenyChildAttach);
            await EnqueueAutomationRunAsync(automation, AutomationRunInfo.StartReason.EntityChanged, changedEntity, tcs);
            await tcs.Task;
        }

        public async Task EnqueueAutomationForManualStartAsync(AutomationEntry automation)
        {
            TaskCompletionSource<bool> tcs =
                new(TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.DenyChildAttach);
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
            // Move this stuff to a background task.
            await Initialization.WaitForEntitiesLoadedAsync();

            foreach (var runOnStartAutomations in _automationRegistry.Automations.Values.Where(a => a.Info.RunOnStart))
            {
                await EnqueueAutomationRunAsync(runOnStartAutomations, AutomationRunInfo.StartReason.Manual, null, null);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var runners = _runners.Values.Where(v => v.IsValueCreated).Select(v => v.Value).ToList();
            return Task.WhenAll(runners.Select(r => r.StopAsync()));
        }

        private Task EnqueueAutomationRunAsync(
            AutomationEntry entry,
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource<bool>? startTcs)
        {
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