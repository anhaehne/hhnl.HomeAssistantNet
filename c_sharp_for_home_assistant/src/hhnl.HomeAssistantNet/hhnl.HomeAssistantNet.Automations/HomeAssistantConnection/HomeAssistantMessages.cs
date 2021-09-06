using hhnl.HomeAssistantNet.Shared.Entities;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public record WebSocketApiMessageBase([property: JsonPropertyName("type")] string Type);

    public record WebsocketApiMessage(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("success")] bool? Success,
        [property: JsonPropertyName("event")] JsonElement Event,
        [property: JsonPropertyName("result")] JsonElement Result,
        [property: JsonPropertyName("error")] WebsocketApiMessageError? Error);

    public record WebSocketAuthRequired([property: JsonPropertyName("type")] string Type = "auth_required");

    public record WebSocketAuth([property: JsonPropertyName("access_token")] string AccessToken, [property: JsonPropertyName("type")] string Type = "auth");

    public record WebSocketAuthOk([property: JsonPropertyName("type")] string Type = "auth_ok");

    public record WebsocketApiMessageError(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message);

    public record StateChangedNotification(
        [property: JsonPropertyName("entity_id")] string EntityId,
        [property: JsonPropertyName("new_state")] JsonElement NewState,
        [property: JsonIgnore] Events.Current SourceEvent) : INotification;
}
