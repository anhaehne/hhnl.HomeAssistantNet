using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using HomeAssistant;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class LivingRoomAutomations
    {
        /// <summary>
        /// Prevent the lights from being turned on before 8pm.
        /// </summary>
        [Automation(runOnStart: true, reentryPolicy:ReentryPolicy.Discard)]
        public async Task TurnOffLivingRoomWhenTurnedOn(MyTestDependency t, Lights.AndreWohnzimmer livingroom)
        {
            if (livingroom.IsOn && DateTime.Now.Hour < 22)
                await livingroom.TurnOffAsync();
        }
    }
}