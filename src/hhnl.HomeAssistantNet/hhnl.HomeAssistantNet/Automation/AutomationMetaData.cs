using System.Collections.Generic;

namespace hhnl.HomeAssistantNet.Automation
{
    public abstract class AutomationMetaData
    {
        protected AutomationMetaData(IReadOnlyCollection<AutomationInfo> automations)
        {
            Automations = automations;
        }

        public IReadOnlyCollection<AutomationInfo> Automations { get; }
    }
}