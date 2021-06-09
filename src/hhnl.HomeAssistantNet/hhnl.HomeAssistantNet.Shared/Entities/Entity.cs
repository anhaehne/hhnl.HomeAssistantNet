using System.Text.Json;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    /// <summary>
    /// The entity base class has a few properties that are common among all entities in Home Assistant.
    /// </summary>
    public class Entity
    {
        public const string AllEntityId = "all";

        public Entity(string uniqueId, IHomeAssistantClient assistantClient)
        {
            HomeAssistantClient = assistantClient;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// The state of the entity. For example: "on".
        /// </summary>
        public string? State => CurrentState?.GetPropertyOrNull("state")?.GetString();

        /// <summary>
        /// Name of the entity.
        /// </summary>
        public string? FriendlyName =>
            CurrentState?.GetPropertyOrNull("attributes")?.GetPropertyOrNull("friendly_name")?.GetString();

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public string UniqueId { get; }

        protected IHomeAssistantClient HomeAssistantClient { get; }

        public JsonElement? CurrentState { get; set; }
    }
}