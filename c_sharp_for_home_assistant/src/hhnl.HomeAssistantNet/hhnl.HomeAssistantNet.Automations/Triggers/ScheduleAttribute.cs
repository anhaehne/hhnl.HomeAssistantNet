using Cronos;
using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace hhnl.HomeAssistantNet.Automations.Triggers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ScheduleAttribute : AutomationTriggerAttributeBase
    {
        private static readonly WeekDay[] _weekDays = new[] { WeekDay.Sunday, WeekDay.Monday, WeekDay.Tuesday, WeekDay.Wednesday, WeekDay.Thursday, WeekDay.Friday, WeekDay.Saturday };
        private readonly CronExpression _cronExpression;
        private IAutomationService? _automationService;
        private IHostApplicationLifetime? _lifetime;
        private ILogger<ScheduleAttribute>? _logger;

        public CronExpression CronExpression => _cronExpression;

        /// <summary>
        /// Creates a new schedule on fixed time on certain days.
        /// </summary>
        /// <param name="days">The days this automation should run.</param>
        /// <param name="hour">The hour this automation should run. (0 - 23)</param>
        /// <param name="minute">The minute of the hour this automation should run. (0 - 59)</param>
        /// <param name="second">The second of the minute this automation should run. (0 - 59)</param>
        public ScheduleAttribute(WeekDay days, int hour, int minute = 0, int second = 0)
        {
            if (hour > 23 || hour < 0)
                throw new ArgumentException("Hour must be between 0 and 23", nameof(hour));
            if (minute > 59 || minute < 0)
                throw new ArgumentException("Minute must be between 0 and 59", nameof(hour));
            if (second > 59 || second < 0)
                throw new ArgumentException("Second must be between 0 and 59", nameof(hour));

            _cronExpression = CronExpression.Parse($"{second} {minute} {hour} ? * {ToCron(days)}", CronFormat.IncludeSeconds);
        }

        /// <summary>
        /// Creates a new schedule repeating every time part.
        /// </summary>
        /// <param name="timePart">The part of the time to repeat on.</param>
        /// <param name="value">The multiplier of the time part.</param>
        /// <example>
        /// timePart = Every.Hour; value = 2 => every 2 hours; starting on the first second of the first minute of the hour.
        /// timePart = Every.Minute; value = 100 => every 100 minutes; starting on the first second of the minute.
        /// </example>
        public ScheduleAttribute(Every timePart, int value = 1)
        {
            if (value < 1)
                throw new ArgumentException("Value must greater than 0", nameof(value));

            switch (timePart)
            {
                case Every.Second:
                    _cronExpression = CronExpression.Parse($"*/{value} * * * * *", CronFormat.IncludeSeconds);
                    break;
                case Every.Minute:
                    _cronExpression = CronExpression.Parse($"0 */{value} * * * *", CronFormat.IncludeSeconds);
                    break;
                case Every.Hour:
                    _cronExpression = CronExpression.Parse($"0 0 */{value} * * *", CronFormat.IncludeSeconds);
                    break;
                case Every.Day:
                    _cronExpression = CronExpression.Parse($"0 0 0 */{value} * *", CronFormat.IncludeSeconds);
                    break;
                case Every.Month:
                    _cronExpression = CronExpression.Parse($"0 0 0 1 */{value} *", CronFormat.IncludeSeconds);
                    break;
                default:
                    throw new ArgumentException("Unknown value", nameof(timePart));
            }
        }

        public ScheduleAttribute(string cronExpression)
        {
            _cronExpression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
        }

        private string ToCron(WeekDay weekDay)
        {
            return string.Join(",", _weekDays.Where(x => weekDay.HasFlag(x)).Select(x => Enum.GetName(typeof(WeekDay), x)!.Substring(0, 3).ToUpper()));
        }

        public override Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider)
        {
            _automationService = automationService;
            _logger = serviceProvider.GetRequiredService<ILogger<ScheduleAttribute>>();
            _lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            ScheduleNextRun(automation);
            return Task.CompletedTask;
        }

        private void ScheduleNextRun(AutomationEntry entry)
        {
            var nextOccurence = CronExpression.GetNextOccurrence(DateTime.UtcNow);

            if (!nextOccurence.HasValue)
            {
                _logger.LogWarning($"Automation {entry.Info.Name} has no next scheduled date for cron expression '{CronExpression}'.");
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

                    await _automationService!.EnqueueAutomationAsync(entry, AutomationRunInfo.StartReason.Schedule);

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

    [Flags]
    public enum WeekDay
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,

        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
    }

    public enum Every
    {
        Hour,
        Minute,
        Second,
        Day,
        Month
    }
}
