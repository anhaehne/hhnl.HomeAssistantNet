using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class SupervisorConnectionInfo
    {
        [JsonConstructor]
        public SupervisorConnectionInfo(string id, bool isRemote, IReadOnlyCollection<AutomationInfoDto> automations, bool isComplete)
        {
            Id = id;
            IsRemote = isRemote;
            Automations = automations;
            IsComplete = isComplete;
        }

        public string Id { get; }

        public bool IsRemote { get; }

        public bool IsComplete { get; set; }

        public IReadOnlyCollection<AutomationInfoDto> Automations { get; set; }
    }
}