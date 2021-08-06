using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Automation;
using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Triggers
{
    /// <summary>
    /// When an automation is decorated with the attribute, the automation will be run whenever the connection to home assistant has been esteblished.
    /// This happens on startup but also when the connection was lost and the client reconnected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RunOnConnectedAttribute : AutomationTriggerAttributeBase
    {
        public override Task RegisterTriggerAsync(AutomationEntry automation, IAutomationService automationService, IServiceProvider serviceProvider)
        {
            HomeAssistantClientConnectedNotification.Handler.RegisterAutomation(automation);

            // We know that we are already connected when we reach this step so we run the automation for the first time.
            return automationService.EnqueueAutomationAsync(automation, AutomationRunInfo.StartReason.RunOnConnect);
        }
    }
}
