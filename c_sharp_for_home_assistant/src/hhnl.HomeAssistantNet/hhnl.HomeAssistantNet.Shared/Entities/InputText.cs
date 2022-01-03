using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_text", "InputTexts")]
    public class InputText : Entity
    {
        public InputText(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        public string? GetValue()
        {
            if (ValueIsUnknown)
                return null;

            return State;
        }

        public async Task SetValueAsync(string value, CancellationToken cancellationToken = default)
        {
            await HomeAssistantClient.CallServiceAsync("input_text", "set_value", new { entity_id = UniqueId, value }, cancellationToken);
        }
    }
}
