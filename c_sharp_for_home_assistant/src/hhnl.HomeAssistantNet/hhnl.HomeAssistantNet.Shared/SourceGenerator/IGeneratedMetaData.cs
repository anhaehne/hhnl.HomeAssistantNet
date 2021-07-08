using System;
using System.Collections.Generic;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.HomeAssistantNet.Shared.SourceGenerator
{
    public interface IGeneratedMetaData
    {
        void RegisterEntitiesAndAutomations(IServiceCollection serviceCollection);

        //IReadOnlyCollection<AutomationInfo> AutomationMetaData { get; }
        
        IReadOnlyCollection<Type> EntityTypes { get; }
    }
}
