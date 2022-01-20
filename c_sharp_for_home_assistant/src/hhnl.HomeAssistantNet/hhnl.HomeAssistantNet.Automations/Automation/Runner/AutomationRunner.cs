using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Utils;
using hhnl.HomeAssistantNet.Shared.Automation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.Automations.Automation.Runner
{
    public abstract class AutomationRunner
    {
        private readonly IServiceProvider _provider;
        protected readonly ILogger<AutomationRunner> _logger;

        protected AutomationRunner(AutomationEntry entry, IServiceProvider provider)
        {
            Entry = entry;
            _provider = provider;
            _logger = _provider.GetRequiredService<ILogger<AutomationRunner>>();
        }

        protected AutomationEntry Entry { get; }

        public abstract Task EnqueueAsync(
            AutomationRunInfo.StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot);

        public virtual void Start()
        {
        }

        public virtual Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async Task StopRunAsync(AutomationRunInfo run)
        {
            run.CancellationTokenSource?.Cancel();
            run.State = AutomationRunInfo.RunState.Cancelled;
            await PublishRunChangedAsync(run);
            await run.Task;
        }

        protected Task PublishRunChangedAsync(AutomationRunInfo run) => _provider.GetRequiredService<IMediator>().Publish(new AutomationRunStateChangedNotification(Entry, run));

        protected AutomationRunInfo CreateAutomationRun(
            AutomationRunInfo.StartReason reason,
            string? reasonMessage,
            TaskCompletionSource? startTcs,
            IReadOnlyDictionary<Type, object> snapshot,
            AutomationRunInfo.RunState initialState = AutomationRunInfo.RunState.Running)
        {
            var run = new AutomationRunInfo
            {
                Id = Guid.NewGuid(),
                Started = DateTimeOffset.Now,
                State = initialState,
                CancellationTokenSource = new CancellationTokenSource(),
                Reason = reason,
                ReasonMessage = reasonMessage,
                EntitySnapshot = snapshot,
            };

            run.Start = () =>
            {
                run.Task = Task.Run(async () =>
                {
                    startTcs?.TrySetResult();

                    using var scope = _provider.CreateScope();
                    
                    try
                    {
                        await PublishRunChangedAsync(run);

                        AutomationRunContext.Current =
                            new AutomationRunContext(run.CancellationTokenSource.Token, scope.ServiceProvider, run);

                        _logger.LogDebug($"Starting automation run '{run.Id}' at: {run.Started} Reasons: '{run.Reason}' Message: '{run.ReasonMessage}'");


                        if (!Initialization.IsHomeAssistantConnected)
                        {
                            _logger.LogWarning($"No home assistant connection. Setting run to '{nameof(AutomationRunInfo.RunState.Cancelled)}'.");
                            run.State = AutomationRunInfo.RunState.Cancelled;
                            await PublishRunChangedAsync(run);
                            return;
                        }

                        await Entry.Info.RunAutomation(scope.ServiceProvider, run.CancellationTokenSource.Token);

                        var endState = run.CancellationTokenSource.Token.IsCancellationRequested
                            ? AutomationRunInfo.RunState.Cancelled
                            : AutomationRunInfo.RunState.Completed;

                        _logger.LogDebug($"Setting run to '{endState}'.");
                        run.State = endState;
                    }
                    catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                    {
                        _logger.LogDebug($"Setting run to '{nameof(AutomationRunInfo.RunState.Cancelled)}'.");
                        run.State = AutomationRunInfo.RunState.Cancelled;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Unhandled exception occurred while executing automation. Setting run to '{nameof(AutomationRunInfo.RunState.Error)}'.");
                        run.State = AutomationRunInfo.RunState.Error;
                        run.Error = e.ToString();
                    }
                    finally
                    {
                        _logger.LogDebug($"The automation run made {run.ServiceCallCount} service calls.");

                        run.Ended = DateTimeOffset.Now;
                        run.CancellationTokenSource.Dispose();
                        run.CancellationTokenSource = null;
                        AutomationLogger.CloseLog();
                    }

                    await PublishRunChangedAsync(run);
                });
            };
            
            return run;
        }
    }
}