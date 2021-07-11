using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Automation;
using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Triggers
{
    /// <summary>
    /// When an automation is decorated with the attribute, the automation will be run on application start.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RunOnStartAttribute : AutomationTriggerAttributeBase
    {
        public override Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider)
        {
            return automationService.EnqueueAutomationAsync(automation, AutomationRunInfo.StartReason.RunOnStart);
        }
    }
}
