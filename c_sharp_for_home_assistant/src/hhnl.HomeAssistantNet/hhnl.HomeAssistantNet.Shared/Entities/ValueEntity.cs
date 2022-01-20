using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.Utils;
using System.Text.Json;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    public abstract class ValueEntity<TValue> : Entity where TValue : struct
    {
        protected ValueEntity(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        /// <summary>
        /// Gets the current value of the entity.
        /// </summary>
        /// <returns>The value or <c>null</c> if the value is unkown.</returns>
        public TValue? GetValue()
        {
            if (ValueIsUnknown)
                return default;

            return Parse(State!);
        }

        protected virtual TValue? Parse(string state)
        {
            try
            {
                return JsonSerializer.Deserialize<TValue>(state);
            }
            catch (JsonException ex)
            {
                throw new StateInvalidException($"Unable to convert the state of entity '{UniqueId}' ('{State?.ToString()}') to type '{typeof(TValue)}'.", ex);
            }
        }
    }
}
