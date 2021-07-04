using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [UniqueId(UniqueId)]
    public class Event
    {
        public const string UniqueId = "hhnl.HomeAssistantNet.Shared.Entities.event";

        public static Event Empty = new(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new object())), string.Empty, default, string.Empty, new EventContext(string.Empty, null, null));

        [JsonConstructor]
        public Event(JsonElement data, string eventType, DateTimeOffset timeFired, string origin, EventContext context)
        {
            Data = data;
            EventType = eventType;
            TimeFired = timeFired;
            Origin = origin;
            Context = context;
        }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }


        [JsonPropertyName("event_type")]
        public string EventType { get; set; }


        [JsonPropertyName("time_fired")]
        public DateTimeOffset TimeFired { get; set; }


        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("context")]
        public EventContext Context { get; set; }
    }

    public class EventContext
    {
        [JsonConstructor]
        public EventContext(string id, string? parentId, string? userId)
        {
            Id = id;
            ParentId = parentId;
            UserId = userId;
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
    }
}
