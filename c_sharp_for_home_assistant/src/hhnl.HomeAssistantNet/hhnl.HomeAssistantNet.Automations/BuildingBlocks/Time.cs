using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;

namespace hhnl.HomeAssistantNet.Automations.BuildingBlocks
{
    public static class Time
    {
        /// <summary>
        /// Waits for the specified amount of time.
        /// Allows run cancellation.
        /// </summary>
        /// <param name="time">The specified amount of time to wait.</param>
        public static Task Wait(TimeSpan time)
        {
            return Task.Delay(time, AutomationRunContext.GetRunContextOrFail().CancellationToken);
        }
    }
}