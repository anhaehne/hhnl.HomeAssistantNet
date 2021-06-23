using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public class ConnectionInfo
    {
        [JsonConstructor]
        public ConnectionInfo(string id, bool isRemote, IReadOnlyCollection<AutomationInfoDto> automations)
        {
            Id = id;
            IsRemote = isRemote;
            Automations = automations;
        }

        public string Id { get; }

        public bool IsRemote { get; }

        public IReadOnlyCollection<AutomationInfoDto> Automations { get; }
    }
}