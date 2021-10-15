using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

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
    }
}
