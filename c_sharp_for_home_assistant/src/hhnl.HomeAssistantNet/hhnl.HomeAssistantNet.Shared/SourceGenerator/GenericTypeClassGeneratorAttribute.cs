using hhnl.HomeAssistantNet.Shared.Entities;
using System;

namespace hhnl.HomeAssistantNet.Shared.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class GenericTypeClassGeneratorAttribute : Attribute
    {
        public abstract (string GenericTypeName, string GenericTypeCode, bool Success) GenerateGenericType(string parentName, EntityPoco entity);
    }
}
