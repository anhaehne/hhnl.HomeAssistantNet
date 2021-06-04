using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automation;
using HomeAssistant;


namespace hhnl.HomeAssistantNet.TestProject
{
    public class TestAutomation
    {
        [Automation]
        public void MyTestAutomation()
        {
            //var t = HomeAssistant.Lights.Badezimmer.AssumedState;
        }
        
        [Automation]
        public void MyTestAutomation2()
        {
            //var t = HomeAssistant.Lights.Badezimmer.AssumedState;
        }
    }
    
    public class TestAutomation2
    {
        [Automation]
        public async Task TurnOffLivingRoomWhenTurnedOn(Lights.AndreWohnzimmer wohnzimmer)
        {
            if(wohnzimmer.IsOn && DateTime.Now.Hour < 20)
            {
                await wohnzimmer.TurnOffAsync();

            }
            //var t = HomeAssistant.Lights.Badezimmer.AssumedState;
        }
    }
}