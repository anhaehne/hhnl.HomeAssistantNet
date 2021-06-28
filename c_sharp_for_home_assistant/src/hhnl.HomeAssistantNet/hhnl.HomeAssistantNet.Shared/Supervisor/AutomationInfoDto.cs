using System.Collections.Generic;
using System.Text.Json.Serialization;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class AutomationInfoDto
    {
        [JsonConstructor]
        public AutomationInfoDto(AutomationInfo info, IReadOnlyCollection<AutomationRunInfo> runs)
        {
            Info = info;
            Runs = runs;
        }

        public AutomationInfo Info { get; }

        public IReadOnlyCollection<AutomationRunInfo> Runs { get; }
    }
}