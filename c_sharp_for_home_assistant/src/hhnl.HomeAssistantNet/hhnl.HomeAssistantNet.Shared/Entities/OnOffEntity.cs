using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    public abstract class OnOffEntity : BinaryEntity
    {
        private readonly string _domain;

        protected OnOffEntity(string uniqueId, IHomeAssistantClient assistantClient, string domain) : base(uniqueId,
            assistantClient)
        {
            _domain = domain;
        }

        public async Task TurnOffAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync(_domain, "turn_off", new { entity_id = UniqueId }, cancellationToken);
        }

        public async Task TurnOnAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync(_domain, "turn_on", new { entity_id = UniqueId }, cancellationToken);
        }

        public async Task ToggleAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync(_domain, "toggle", new { entity_id = UniqueId }, cancellationToken);
        }

        public Task SetAsync(bool on, CancellationToken cancellationToken = default)
        {
            return on ? TurnOnAsync(cancellationToken) : TurnOffAsync(cancellationToken);
        }
    }
}