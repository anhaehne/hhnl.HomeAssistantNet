using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("binary_sensor", "BinarySensors")]
    public abstract class BinarySensor : Entity
    {
        protected BinarySensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
        
        public bool IsOn => State == "on";
        
        public bool IsOff => State == "off";

    }
}