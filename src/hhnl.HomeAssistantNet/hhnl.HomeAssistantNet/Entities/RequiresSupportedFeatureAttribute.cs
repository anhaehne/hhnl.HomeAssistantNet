using System;
using RestSharp;

namespace hhnl.HomeAssistantNet.Entities
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class RequiresSupportedFeatureAttribute : Attribute
    {
        public SupportedFeature RequiredFeatures { get; }

        public RequiresSupportedFeatureAttribute(SupportedFeature requiredFeatures)
        {
            RequiredFeatures = requiredFeatures;
        }
    }
}