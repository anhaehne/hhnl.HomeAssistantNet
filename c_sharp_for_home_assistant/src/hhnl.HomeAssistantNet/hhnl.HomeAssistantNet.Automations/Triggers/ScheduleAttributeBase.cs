using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
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

        private IAutomationService? _automationService;
        private IHostApplicationLifetime? _lifetime;
        private ILogger<ScheduleAttributeBase>? _logger;
        private readonly string _name;

        public override Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider)
        {
            _automationService = automationService;
            _logger = serviceProvider.GetRequiredService<ILogger<ScheduleAttributeBase>>();
            _lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            ScheduleNextRun(automation);
            return Task.CompletedTask;
        }

        protected abstract DateTime? GetNextOccurrence();

        private void ScheduleNextRun(AutomationEntry entry)
        {
            var nextOccurence = GetNextOccurrence();

            if (!nextOccurence.HasValue)
            {
                _logger.LogWarning($"Automation {entry.Info.Name} has no next scheduled date for the schedule '{_name}'.");
                return;
            }

            var runIn = nextOccurence.Value - DateTime.Now;

            // Make sure we don't get invalid intervals.
            if (runIn.TotalMilliseconds < 1)
                runIn = TimeSpan.FromMilliseconds(1);

            Timer? t = new(runIn.TotalMilliseconds);
            t.Elapsed += ScheduleRun;
            t.Start();

            async void ScheduleRun(object sender, ElapsedEventArgs e)
            {
                try
                {
                    t.Stop();
                    t.Elapsed -= ScheduleRun;

                    if ((_lifetime?.ApplicationStopping ?? default).IsCancellationRequested)
                    {
                        return;
                    }

                    await _automationService!.EnqueueAutomationAsync(entry, AutomationRunInfo.StartReason.Schedule, $"Schedule: {_name}");

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
    }
}
