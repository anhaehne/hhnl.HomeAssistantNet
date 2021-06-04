using System;
using System.Linq;
using System.Reflection;

namespace hhnl.HomeAssistantNet.Automation
{
    public abstract class AutomationStartup
    {
        private readonly Assembly _assembly;

        public AutomationStartup(Type? assemblyToSearchIn = null)
        {
            _assembly = assemblyToSearchIn?.Assembly ?? Assembly.GetEntryAssembly()!;
        }
        
        public void Run(string[] args)
        {
            var metaData = GetMetaData();

            
        }

        private AutomationMetaData GetMetaData()
        {
            var metaDataType = _assembly.GetTypes().Single(t => t.Name == "AutomationMetaData_Generated");

            return (AutomationMetaData)Activator.CreateInstance(metaDataType);
        }
    }
}