using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using System;
using System.Collections.Generic;
using System.Text;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    public abstract class BinaryEntity : ValueEntity<bool>
    {
        protected BinaryEntity(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool IsOn => State == "on";

        public bool IsOff => State == "off";

        protected override bool? Parse(string state) => state switch
        {
            "on" => true,
            "off" => false,
            _ => null,
        };
    }
}
