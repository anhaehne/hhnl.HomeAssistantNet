using System;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    public abstract class Light : Entity
    {
        [Flags]
        public enum SupportedFeatures
        {
            SupportBrightness = 1,
            SupportColorTemp = 2,
            SupportEffect = 4,
            SupportFlash = 8,
            SupportColor = 16,
            SupportTransition = 32,
            SupportWhiteValue = 128
        }

        public Light(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool IsOn => State == "on";

        public async Task TurnOffAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("light", "turn_off", new { entity_id = UniqueId }, cancellationToken);
        }

        public async Task TurnOnAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("light", "turn_on", new { entity_id = UniqueId }, cancellationToken);
        }

        public async Task ToggleAsync(CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("light", "toggle", new { entity_id = UniqueId }, cancellationToken);
        }
    }
}