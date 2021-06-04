using hhnl.HomeAssistantNet.Automation;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class Startup : AutomationStartup
    {
        public static void Main(string[] args)
        {
            new Startup().Run(args);
        }
    }
}