using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("device_tracker", "DeviceTrackers")]
    public abstract class DeviceTracker : HomeAwayEntity
    {
        protected DeviceTracker(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
    }
}