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
        public string? FriendlyName => GetAttributeOrDefault<string>("friendly_name");

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public string UniqueId { get; }

        public T? GetAttributeOrDefault<T>(string attributeName)
        {
            var attribute = CurrentState?.GetPropertyOrNull("attributes")?.GetPropertyOrNull(attributeName);
            return attribute.ToObject<T>() ?? default;
        }

        //public T? GetAttributeOrDefault<T>(string attributeName) where T : class
        //{
        //    var attribute = CurrentState?.GetPropertyOrNull("attributes")?.GetPropertyOrNull(attributeName);
        //    return attribute?.ToObject<T>() ?? default;
        //}

        protected IHomeAssistantClient HomeAssistantClient { get; }

        public JsonElement? CurrentState { get; set; }
    }
}