using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("switch", "Switches")]
    public abstract class Switch : OnOffEntity
    {
        protected Switch(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient, "switch")
        {
            
        }
    }
}