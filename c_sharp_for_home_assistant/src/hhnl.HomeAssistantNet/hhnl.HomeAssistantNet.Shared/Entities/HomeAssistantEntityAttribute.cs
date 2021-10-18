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
            bool supportsAllEntity = false,
            int priority = 0)
        {
            Domain = domain;
            ContainingEntityClass = containingEntityClass;
            SupportedFeaturesEnumType = supportedFeaturesEnumType;
            SupportsAllEntity = supportsAllEntity;
            Priority = priority;
        }

        public string Domain { get; }

        public string ContainingEntityClass { get; }

        public Type? SupportedFeaturesEnumType { get; }

        public bool SupportsAllEntity { get; }

        public int Priority { get; }
    }
}