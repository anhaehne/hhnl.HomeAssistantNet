using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{ 
    public abstract class HomeAwayEntity : Entity
    {
        protected HomeAwayEntity(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool IsHome => base.State == "home";
        
        public bool IsAway => base.State == "away";
    }
}