using System;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class RequiresSupportedFeatureAttribute : Attribute
    {
        public object RequiredFeatures { get; }

        public RequiresSupportedFeatureAttribute(object requiredFeatures)
        {
            RequiredFeatures = requiredFeatures;
        }
    }
}