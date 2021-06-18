using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using HomeAssistant;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class LivingRoomAutomations
    {
        /// <summary>
        /// Prevent the lights from being turned on before 8pm.
        /// </summary>
        [Automation(reentryPolicy:ReentryPolicy.Discard)]
        public async Task TurnOffOfficeWhenTurnedOnBeforeSunSet(Lights.Buro office, Entities.SunSun sun)
        {
            if (office.IsOn && sun.State != "below_horizon")
                await office.TurnOffAsync();
            
        }
    }
}