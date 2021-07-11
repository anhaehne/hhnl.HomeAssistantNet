using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Triggers
{
    public abstract class AutomationTriggerAttributeBase : Attribute
    {
        public abstract Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider);
    }
}
