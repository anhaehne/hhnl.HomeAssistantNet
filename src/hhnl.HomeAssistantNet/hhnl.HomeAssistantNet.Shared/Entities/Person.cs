using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("person", "Persons")]
    public abstract class Person : HomeAwayEntity
    {
        protected Person(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
    }
}