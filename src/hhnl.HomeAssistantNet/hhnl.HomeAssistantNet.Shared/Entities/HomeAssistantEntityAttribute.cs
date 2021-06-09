using System;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HomeAssistantEntityAttribute : Attribute
    {
        public HomeAssistantEntityAttribute(
            string domain,
            string containingEntityClass,
            Type? supportedFeaturesEnumType = null,
            bool supportsAllEntity = false)
        {
            Domain = domain;
            ContainingEntityClass = containingEntityClass;
            SupportedFeaturesEnumType = supportedFeaturesEnumType;
            SupportsAllEntity = supportsAllEntity;
        }

        public string Domain { get; }

        public string ContainingEntityClass { get; }

        public Type? SupportedFeaturesEnumType { get; }

        public bool SupportsAllEntity { get; }
    }
}