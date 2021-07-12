using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace hhnl.HomeAssistantNet.Automations.Triggers
{
    public abstract class ScheduleAttributeBase : AutomationTriggerAttributeBase
    {
        protected ScheduleAttributeBase(string name)
        {
            _name = name;
        }

        private readonly AsyncAutoResetEvent? _trigger = new(true);

        private IAutomationService? _automationService;
        private IHostApplicationLifetime? _lifetime;
        private ILogger<ScheduleAttributeBase>? _logger;
        private static readonly Random _r = new();

        private AutomationEntry? _automation;
        private CancellationTokenSource? _cts;
        private Task? _runTask;

        private readonly string _name;

        public override Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider)
        {
            _automation = automation;
            _automationService = automationService;
            _logger = serviceProvider.GetRequiredService<ILogger<ScheduleAttributeBase>>();
            _lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            _cts = new();
            _runTask = RunAsync();
            return Task.CompletedTask;
        }

        public async override Task UnregisterTriggerAsync()
        {
            _cts?.Cancel();
            if (_runTask is not null)
                await _runTask;
            _cts?.Dispose();
        }

        protected abstract DateTime? GetNextOccurrence();

        private async Task RunAsync()
        {
            if (_cts is null)
                throw new InvalidOperationException($"{nameof(_cts)} is null");
            if (_trigger is null)
                throw new InvalidOperationException($"{nameof(_trigger)} is null");

            await Task.Yield();

            while(!_cts.IsCancellationRequested)
            {
                await _trigger.WaitAsync(_cts.Token);

                if (_cts.IsCancellationRequested)
                    return;

                await ScheduleNextRunAsync();
            }
        }

        private async Task ScheduleNextRunAsync()
        {
            var nextOccurence = GetNextOccurrence();

            if (!nextOccurence.HasValue)
            {
                _logger.LogWarning($"Automation {_automation!.Info.Name} has no next scheduled date for the schedule '{_name}'.");
                return;
            }

            var runIn = nextOccurence.Value - DateTime.Now;

            if(runIn.TotalMilliseconds < 0)
            {
                // Waiting a random amount of time, between 2 und 7 seconds, and trying again.
                var wait = _r.Next(2, 7);
                _logger.LogWarning($"Automation {_automation!.Info.Name} next occurence is invalid: '{runIn}'. Waiting {wait} seconds and trying again.");
                await Task.Delay(TimeSpan.FromSeconds(wait), _cts!.Token);
                _trigger!.Set();
                return;
            } 
            else if (runIn.TotalMilliseconds < 1)
            {
                // Timer only accepts values >= 1
                runIn = TimeSpan.FromMilliseconds(1);
            }

            System.Timers.Timer? t = new(runIn.TotalMilliseconds);
            t.Elapsed += ScheduleRun;
            t.Start();

            async void ScheduleRun(object sender, ElapsedEventArgs e)
            {
                try
                {
                    t.Stop();
                    t.Elapsed -= ScheduleRun;
                    t.Dispose();

                    if ((_lifetime?.ApplicationStopping ?? default).IsCancellationRequested)
                    {
                        return;
                    }

                    await _automationService!.EnqueueAutomationAsync(_automation!, AutomationRunInfo.StartReason.Schedule, $"Schedule: {_name}");

                    _trigger!.Set();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occured while enqueueing the scheduled next automation run of '{_automation!.Info.Name}'.");
                }
                finally
                {
                    t.Dispose();
                }
            }
        }
    }
}
