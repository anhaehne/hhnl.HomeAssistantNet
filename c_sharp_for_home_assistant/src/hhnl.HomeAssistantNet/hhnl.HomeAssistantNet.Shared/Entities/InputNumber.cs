using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System.Text.Json;
using static hhnl.HomeAssistantNet.Shared.Entities.Sensor;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_number", "InputNumbers")]
    public class InputNumber : Entity
    {
        public InputNumber(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        public double? GetValue()
        {
            if (ValueIsUnknown)
                return null;

            try
            {
                return JsonSerializer.Deserialize<double>(State!);
            }
            catch (JsonException ex)
            {
                throw new SensorStateInvalidException($"Unable to convert input number state '{State?.ToString()}' to type '{typeof(double)}'.", ex);
            }
        }
    }
}
