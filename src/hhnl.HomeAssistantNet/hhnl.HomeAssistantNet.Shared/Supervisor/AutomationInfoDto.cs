using System.Collections.Generic;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class AutomationInfoDto
    {
        public AutomationInfo Info { get; set; }

        public IReadOnlyCollection<AutomationRunInfo> Runs { get; set; }
    }
}