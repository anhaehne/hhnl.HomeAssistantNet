using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.HomeAssistantConnection
{
    public record WebsocketApiMessage([property: JsonPropertyName("id")]long Id,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("success")] bool? Success,
        [property: JsonPropertyName("event")] JsonElement Event,
        [property: JsonPropertyName("result")] JsonElement Result,
        [property: JsonPropertyName("error")] WebsocketApiMessageError? Error);

    public class WebsocketApiMessageError
    {
        [JsonPropertyName("code")] public string? Code { get; set; }

        [JsonPropertyName("message")] public string? Message { get; set; }
    }
}
