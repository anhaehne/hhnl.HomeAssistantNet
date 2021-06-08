using System;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedFeaturesAttribute : Attribute
    {
        public object? Features { get; }

        public SupportedFeaturesAttribute(object? features)
        {
            Features = features;
        }
    }
}