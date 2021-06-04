using System;

namespace hhnl.HomeAssistantNet.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UniqueIdAttribute : Attribute
    {
        public UniqueIdAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}