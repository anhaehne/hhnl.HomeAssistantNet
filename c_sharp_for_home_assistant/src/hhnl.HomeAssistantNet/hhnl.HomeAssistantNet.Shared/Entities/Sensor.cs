using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System;
using System.Text.Json;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("sensor", "Sensors")]
    public abstract class Sensor : Entity
    {
        protected Sensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        /// <summary>
        /// Deserializes the state into the given type.
        /// Returns <c>default</c> if the State is null or the value is 'unknown'.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <returns>The value or <c>default</c></returns>
        public T? GetValue<T>()
        {
            if (ValueIsUnknown)
            {
                return default;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(State!);
            }
            catch (JsonException ex)
            {
                throw new SensorStateInvalidException($"Unable to convert sensor state '{State?.ToString()}' to type '{typeof(T)}'.", ex);
            }
        }

        public class SensorStateInvalidException : Exception
        {
            public SensorStateInvalidException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
    }
}