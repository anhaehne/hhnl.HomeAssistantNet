using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HADotNet.Core.Models;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.SourceGenerator
{
    public class EntityClassSourceFileGenerator
    {
        private readonly GeneratorExecutionContext _context;

        public EntityClassSourceFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void AddEntityClass(
            string className,
            string entityClass,
            Type? supportedFeaturesType,
            IEnumerable<StateObject> entities,
            bool removeDomain = true)
        {
            var entityClasses = entities.Select(entity =>
            {
                var entityName = entity.Attributes.ContainsKey("friendly_name")
                    ? $"{entity.Attributes["friendly_name"]} ({entity.EntityId})"
                    : entity.EntityId;

                var supportedFeatures = entity.Attributes.ContainsKey("supported_features") && supportedFeaturesType is not null
                    ? string.Join(" | ", GetSupportedFeatures(entity, supportedFeaturesType))
                    : "null";

                var className = ToClassName(entity.EntityId);
                
                return $@"
        /// <summary>
        /// The entity {entityName}
        /// </summary>
        [UniqueId(""{entity.EntityId}"")]
        [SupportedFeatures({supportedFeatures})]
        public class {className}: {entityClass}
        {{
            public {className}() : base(""{entity.EntityId}"")
            {{
            }}
        }}";
            });


            var source = @$"
using System;
using hhnl.HomeAssistantNet.Entities;
namespace HomeAssistant
{{
    public static class {className}
    {{
        {string.Join(Environment.NewLine, entityClasses)}
    }}
}}";
            
            _context.AddSource($"HomeAssistant_{className}", source);

            string ToClassName(string entity)
            {
                return string.Join("", RemoveDomain(entity).Split('.', '_', '-').Select(FirstToUpper));
            }

            static string FirstToUpper(string input)
            {
                return input.First().ToString().ToUpper() + input.Substring(1);
            }

            string RemoveDomain(string entity)
            {
                if (!removeDomain)
                    return entity;

                var index = entity.IndexOf('.');

                if (index == -1)
                    return entity;

                return entity.Substring(index + 1);
            }

            static IEnumerable<string> GetSupportedFeatures(StateObject state, Type supportedFeaturesType)
            {
                if (!state.Attributes.ContainsKey("supported_features"))
                    yield break;

                var stringValue = state.Attributes["supported_features"].ToString();

                if (stringValue is null)
                    yield break;

                if (!int.TryParse(stringValue, out var intValue))
                    yield break;

                if (intValue < 1)
                    yield break;

                foreach (var value in Enum.GetValues(supportedFeaturesType))
                {
                    var currentIntValue = (int)value;

                    if ((intValue & currentIntValue) == currentIntValue)
                        yield return $"{GetFullAccessName(supportedFeaturesType)}.{Enum.GetName(supportedFeaturesType, value)}";
                }
            }

            static string GetFullAccessName(Type t)
            {
                return t.DeclaringType is null ? t.Name : $"{GetFullAccessName(t.DeclaringType)}.{t.Name}";
            }
        }
    }
}