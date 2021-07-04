using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("script", "Scripts")]
    public abstract class Script : Entity
    {
        protected Script(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool IsOn => State == "on";

        public bool IsOff => State == "off";

        public async Task TurnOnAsync(dynamic? data = null, CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("script", "turn_on", UniqueId, data, cancellationToken);
        }
    }
}
