using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public class QueueLatestAutomationRunner : AutomationRunner
    {
        private readonly CancellationTokenSource _cts = new();

        private Task _runTask = Task.CompletedTask;
        private AutomationRunInfo? _next;

        private new readonly ILogger<QueueLatestAutomationRunner> _logger;
        private readonly AsyncAutoResetEvent _runTrigger = new();
        private readonly SemaphoreSlim _nextSemaphore = new(1);

        public QueueLatestAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
            _logger = provider.GetRequiredService<ILogger<QueueLatestAutomationRunner>>();
        }

        public override void Start()
        {
            _runTask = Run();
            _runTask.ContinueWith(task =>
            {
                if (!_cts.IsCancellationRequested)
                    _logger.LogError("The run task has completed even though the runner hasn't been stopped.");
            });
        }

        public override async Task StopAsync()
        {
            _cts.Cancel();
            Entry.LatestRun?.CancellationTokenSource?.Cancel();

            await _runTask.IgnoreCancellationAsync();
        }

        public override async Task StopRunAsync(AutomationRunInfo run)
        {
            await _nextSemaphore.WaitAsync();

            try
            {
                // If the run is the next run, remove it from the queue
                if (_next is not null && _next == run)
                    _next = null;
            }
            finally
            {
                _nextSemaphore.Release();
            }

            await base.StopRunAsync(run);
        }

        public override async Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot)
        {
            _logger.LogTrace("Create automation");
            var run = CreateAutomationRun(reason, reasonMessage, startTcs, snapshot, AutomationRunInfo.RunState.WaitingInQueue);

            if (!_nextSemaphore.Wait(0))
            {
                _logger.LogTrace("Waiting for semaphore to enqueue run");
                await _nextSemaphore.WaitAsync(_cts.Token);
            }

            try
            {
                // Cancel previously enqueued run
                if (_next is not null)
                {
                    _logger.LogTrace("Cancelling previous run");
                    _next.State = AutomationRunInfo.RunState.Cancelled;
                    await PublishRunChangedAsync(_next);
                }

                // Enqueue next run
                _logger.LogTrace("Adding run {0}", run.Id);
                _next = run;
                Entry.AddRun(run);

                startTcs?.TrySetResult();
                await PublishRunChangedAsync(run);

                _logger.LogTrace("Set run trigger");
                // Signal run task
                _runTrigger.Set();
            }
            finally
            {
                _nextSemaphore.Release();
            }
        }

        private async Task Run()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _logger.LogTrace("Waiting for next automation");
                // Wait for the next run
                await _runTrigger.WaitAsync(_cts.Token);

                if (_cts.Token.IsCancellationRequested)
                {
                    return;
                }

                // Wait until we can read the next run
                if (!_nextSemaphore.Wait(0))
                {
                    _logger.LogTrace("Waiting for semaphore to dequeue run");
                    await _nextSemaphore.WaitAsync(_cts.Token);
                }

                AutomationRunInfo? next;

                try
                {
                    next = _next;
                    _next = null;

                    _logger.LogTrace("Dequeue complete");
                }
                finally
                {
                    _nextSemaphore.Release();
                }

                // Check if the next has been canceled.
                if (next is null)
                {
                    _logger.LogTrace("next is null");
                    continue;
                }

                _logger.LogTrace("Starting next run");
                next.Start();
                next.State = AutomationRunInfo.RunState.Running;
                await next.Task;
                _logger.LogTrace("Run complete");
            }

            _logger.LogTrace("Runner has been cancelled");
        }
    }
}