using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.Automations.BuildingBlocks
{
    public class Time
    {
        private Time()
        {

        }

        /// <summary>
        /// Waits for the specified amount of time.
        /// Allows run cancellation.
        /// </summary>
        /// <param name="time">The specified amount of time to wait.</param>
        public static Task Wait(TimeSpan time)
        {
            AutomationRunContext.GetRunContextOrFail().ServiceProvider.GetRequiredService<ILogger<Time>>().LogDebug($"Waiting for {time}");
            return Task.Delay(time, AutomationRunContext.GetRunContextOrFail().CancellationToken);
        }
    }
}