using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    public static class Events
    {
        public static Current Empty = new Current(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new object())), string.Empty, default, string.Empty, new EventContext(string.Empty, null, null));

        [UniqueId(UniqueId)]
        public class Current
        {
            public const string UniqueId = "hhnl.HomeAssistantNet.Shared.Entities.Events.Current";

            [JsonConstructor]
            public Current(JsonElement data, string eventType, DateTimeOffset timeFired, string origin, EventContext context)
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

            public override bool Equals(object? obj)
            {
                return obj is Current @event &&
                       EqualityComparer<JsonElement>.Default.Equals(Data, @event.Data) &&
                       EventType == @event.EventType &&
                       TimeFired.Equals(@event.TimeFired) &&
                       Origin == @event.Origin &&
                       EqualityComparer<EventContext>.Default.Equals(Context, @event.Context);
            }

            public override int GetHashCode()
            {
                int hashCode = -120779909;
                hashCode = hashCode * -1521134295 + Data.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EventType);
                hashCode = hashCode * -1521134295 + TimeFired.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Origin);
                hashCode = hashCode * -1521134295 + EqualityComparer<EventContext>.Default.GetHashCode(Context);
                return hashCode;
            }

            public static bool operator == (Current? left, Current? right)
            {
                return EqualityComparer<Current?>.Default.Equals(left, right);
            }

            public static bool operator != (Current? left, Current? right)
            {
                return !(left == right);
            }
        }

        [UniqueId(UniqueId)]
        public class Any : Current
        {
            public new const string UniqueId = "hhnl.HomeAssistantNet.Shared.Entities.Events.Any";

            public Any(Current current) : base(current.Data, current.EventType, current.TimeFired, current.Origin, current.Context)
            {
            }
        }
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

        public override bool Equals(object? obj)
        {
            return obj is EventContext context &&
                   Id == context.Id &&
                   ParentId == context.ParentId &&
                   UserId == context.UserId;
        }

        public override int GetHashCode()
        {
            int hashCode = -1903632377;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(ParentId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(UserId);
            return hashCode;
        }
    }
}
