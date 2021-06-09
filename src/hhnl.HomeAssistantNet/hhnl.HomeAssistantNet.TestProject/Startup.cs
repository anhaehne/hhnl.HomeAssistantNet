using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.TestProject
{
    public class Startup : AutomationStartup
    {
        public static Task Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            
            
            return new Startup().RunAsync(args);
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            
            // TODO: fix workaround
            if(args.Name.StartsWith("hhnl.HomeAssistantNet.Shared"))
            {
                var path = Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location)!, "hhnl.HomeAssistantNet.Shared.dll");
                var assembly = Assembly.LoadFrom(path);
                return assembly;
            }

            return null;
        }
    }
}