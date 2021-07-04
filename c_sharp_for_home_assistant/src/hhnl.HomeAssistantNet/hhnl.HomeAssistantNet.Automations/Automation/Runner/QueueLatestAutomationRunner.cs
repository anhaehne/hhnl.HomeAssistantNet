using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
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
        private (AutomationRunInfo RunInfo, TaskCompletionSource? StartTcs) _next;

        private readonly AsyncAutoResetEvent _runTrigger = new();
        private readonly SemaphoreSlim _nextSemaphore = new(1);

        public QueueLatestAutomationRunner(AutomationEntry entry, IServiceProvider provider) : base(entry, provider)
        {
        }

        public override void Start()
        {
            _runTask = Run();
        }

        public override async Task StopAsync()
        {
            _cts.Cancel();
            Entry.LatestRun?.CancellationTokenSource?.Cancel();

            await _runTask.IgnoreCancellationAsync();
        }

        public override async Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? changedEntity,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot)
        {
            AutomationRunInfo? run = CreateAutomationRun(reason, changedEntity, startTcs, snapshot, AutomationRunInfo.RunState.WaitingInQueue);

            await _nextSemaphore.WaitAsync(_cts.Token);

            try
            {
                // Cancel previously enqueued run
                if (_next.RunInfo is not null)
                    _next.RunInfo.State = AutomationRunInfo.RunState.Cancelled;

                _next.StartTcs?.SetResult();

                // Enqueue next run
                _next = (run, startTcs);
                Entry.AddRun(run);

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
                // Wait for the next run
                await _runTrigger.WaitAsync(_cts.Token);

                if (_cts.Token.IsCancellationRequested)
                {
                    return;
                }

                // Wait until we can read the next run
                await _nextSemaphore.WaitAsync(_cts.Token);

                AutomationRunInfo? next;

                try
                {
                    next = _next.RunInfo;
                    _next = default;
                }
                finally
                {
                    _nextSemaphore.Release();
                }

                if (next is null)
                {
                    throw new InvalidOperationException("_next can't be null when the next run is triggerd.");
                }

                next.Start();
                next.State = AutomationRunInfo.RunState.Running;
                await next.Task;
            }
        }
    }
}