using System.Text.Json;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("sensor", "Sensors")]
    public abstract class Sensor : Entity
    {
        protected Sensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public T? GetValue<T>()
        {
            return CurrentState.ToObject<T>();
        }
    }
}