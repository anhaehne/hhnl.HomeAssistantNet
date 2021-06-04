using System;

namespace hhnl.HomeAssistantNet.Entities
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SupportedFeaturesAttribute : Attribute
    {
        public object Features { get; }

        public SupportedFeaturesAttribute(object features)
        {
            Features = features;
        }
    }
}