using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace hhnl.HomeAssistantNet.Shared.Entities
{

    [HomeAssistantEntity("sensor", "Sensors", priority: 1)]
    public abstract class NumericSensor : ValueEntity<double>
    {
        private static readonly string[] _deviceClasses = new[] 
        { 
            "aqi", 
            "battery", 
            "carbon_dioxide", 
            "carbon_monoxide", 
            "current", 
            "energy", 
            "gas", 
            "humidity", 
            "illuminance", 
            "monetary",
            "nitrogen_dioxide", 
            "nitrogen_monoxide", 
            "nitrous_oxide", 
            "ozone", 
            "pm1", 
            "pm10", 
            "pm25", 
            "power_factor", 
            "power", 
            "pressure", 
            "signal_strength", 
            "sulphur_dioxide", 
            "temperature", 
            "volatile_organic_compounds", 
            "voltage"
        };

        protected NumericSensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public static bool Filter(IReadOnlyDictionary<string, object?> attributes)
        {
            return attributes.TryGetValue("device_class", out var deviceClass) && deviceClass is string && _deviceClasses.Contains(deviceClass);
        }
    }

    [HomeAssistantEntity("sensor", "Sensors", priority: 2)]
    public abstract class DateTimeSensor : ValueEntity<DateTime>
    {
        private static readonly string[] _deviceClasses = new[] { "date", "timestamp" };

        protected DateTimeSensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public static bool Filter(IReadOnlyDictionary<string, object?> attributes)
        {
            return attributes.TryGetValue("device_class", out var deviceClass) && deviceClass is string && _deviceClasses.Contains(deviceClass);
        }

        protected override DateTime? Parse(string state) => DateTime.Parse(state);
    }

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
                throw new StateInvalidException($"Unable to convert sensor state '{State?.ToString()}' to type '{typeof(T)}'.", ex);
            }
        }
    }

    [HomeAssistantEntity("binary_sensor", "Sensors")]
    public abstract class BinarySensor : BinaryEntity
    {
        protected BinarySensor(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }
    }
}