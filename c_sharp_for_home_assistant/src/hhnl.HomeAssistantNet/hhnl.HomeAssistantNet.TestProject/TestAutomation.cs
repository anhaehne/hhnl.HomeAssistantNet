using hhnl.HomeAssistantNet.Automations.BuildingBlocks;
using hhnl.HomeAssistantNet.Shared.Automation;
using HomeAssistant;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class LivingRoomAutomations
    {
        /// <summary>
        /// Prevent the lights from being turned on before 8pm.
        /// </summary>
        //[Automation(runOnStart: true)]
        public async Task TurnOffOfficeWhenTurnedOnBeforeSunSet(Lights.Buro office, Entities.SunSun sun)
        {
            if (office.IsOn && sun.State != "below_horizon")
                await office.TurnOffAsync();
        }

        [Automation(displayName: "Infinate run automation")]
        public async Task InfiniteRun(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Time.Wait(TimeSpan.FromSeconds(2));   
            }
        }
    }
}