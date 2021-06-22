using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;

namespace CS4HA
{
    public class Startup : AutomationStartup
    {
        public static Task Main(string[] args)
        {
            return new Startup().RunAsync(args);
        }
    }
}