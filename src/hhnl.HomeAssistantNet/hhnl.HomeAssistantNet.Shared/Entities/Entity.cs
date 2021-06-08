using System.Text.Json;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    /// <summary>
    /// The entity base class has a few properties that are common among all entities in Home Assistant.
    /// </summary>
    public class Entity
    {
        public Entity(string uniqueId, IHomeAssistantClient assistantClient)
        {
            HomeAssistantClient = assistantClient;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// The state of the entity. For example: "on".
        /// </summary>
        public string? State { get; private set; }

        /// <summary>
        /// Name of the entity.
        /// </summary>
        public string? FriendlyName { get; private set; }

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public string? UniqueId { get; }

        protected IHomeAssistantClient HomeAssistantClient { get; }

        public virtual void Update(JsonElement json)
        {
            State = json.GetPropertyOrNull("state")?.GetString();
            FriendlyName = json.GetPropertyOrNull("attributes")?.GetPropertyOrNull("friendly_name")?.GetString();
        }
    }
}