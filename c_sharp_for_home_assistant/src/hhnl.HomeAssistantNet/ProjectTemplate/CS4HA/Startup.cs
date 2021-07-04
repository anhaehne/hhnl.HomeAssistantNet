using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CS4HA
{
    public class Startup : AutomationStartup
    {
        public static Task Main(string[] args)
        {
            return new Startup().RunAsync(args);
        }

        protected override void ConfigureServices(HostBuilderContext builderContext, IServiceCollection serviceCollection)
        {
            // You can register custom services here.
        }
    }
}