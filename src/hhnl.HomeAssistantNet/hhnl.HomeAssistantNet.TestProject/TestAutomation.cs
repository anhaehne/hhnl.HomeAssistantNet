using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automation;


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
        public void Temp()
        {
            //var t = HomeAssistant.Lights.Badezimmer.AssumedState;
        }
    }
}