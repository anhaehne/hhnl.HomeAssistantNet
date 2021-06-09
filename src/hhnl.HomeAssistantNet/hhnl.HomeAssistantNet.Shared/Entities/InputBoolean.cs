using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_boolean", "InputBooleans")]
    public abstract class InputBoolean : OnOffEntity
    {
        protected InputBoolean(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient, "input_boolean")
        {
        }
    }
}