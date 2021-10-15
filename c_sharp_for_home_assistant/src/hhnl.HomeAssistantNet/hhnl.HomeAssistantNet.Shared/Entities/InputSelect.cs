using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System;
using System.Collections.Generic;
using System.Text;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_select", "InputSelects")]
    public class InputSelect : Entity
    {
        public InputSelect(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        public string? GetValue()
        {
            if (ValueIsUnknown)
                return null;

            return State;
        }
    }
}
