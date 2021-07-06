using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HADotNet.Core.Models;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public class EntityClassSourceFileGenerator
    {
        public const string EntityNamespace = "HomeAssistant";

        private readonly StateObject _allEntity = new()
        {
            EntityId = Entity.AllEntityId,
            Attributes = new Dictionary<string, object>
            {
                { "friendly_name", "all. Represents all entities of this type." },
                { "supported_features", -1 }
            }
        };

        private readonly GeneratorExecutionContext _context;

        public EntityClassSourceFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public IReadOnlyCollection<string> AddEntityClass(
            string className,
            Type entityBaseClass,
            Type? supportedFeaturesType,
            IEnumerable<StateObject> entities,
            bool removeDomain = true,
            bool supportsAllEntity = false)
        {
            List<string> entitiesFullNames = new();

            if (supportsAllEntity)
                entities = entities.Append(_allEntity);

            var entityClasses = entities.Select(entity =>
            {
                var entityName = entity.Attributes.ContainsKey("friendly_name")
                    ? $"{entity.Attributes["friendly_name"]} ({entity.EntityId})"
                    : entity.EntityId;

                var supportedFeatures = entity.Attributes.ContainsKey("supported_features") && supportedFeaturesType is not null
                    ? string.Join(" | ", GetSupportedFeatures(entity, supportedFeaturesType))
                    : "null";

                // When the entity has a supported features but the collection is empty.
                if (string.IsNullOrEmpty(supportedFeatures))
                    supportedFeatures = "null";

                var entityClassName = ToClassName(entity.EntityId);

                entitiesFullNames.Add($"{EntityNamespace}.{className}.{entityClassName}");

                return $@"
        /// <summary>
        /// The entity {entityName}
        /// </summary>
        [{typeof(UniqueIdAttribute).GetFullName()}(""{entity.EntityId}"")]
        [{typeof(SupportedFeaturesAttribute).GetFullName()}({supportedFeatures})]
        public class {entityClassName}: {entityBaseClass.GetFullName()}
        {{
            public {entityClassName}({typeof(IHomeAssistantClient)} client) : base(""{entity.EntityId}"", client)
            {{
            }}
        }}";
            });


            var source = @$"
using System;
namespace {EntityNamespace}
{{
    public static class {className}
    {{
        {string.Join(Environment.NewLine, entityClasses)}
    }}
}}";

            _context.AddSource($"HomeAssistant_{className}", source);

            return entitiesFullNames;

            string ToClassName(string entity)
            {
                return EnsureStartsWithValidCharacter(string.Join("", RemoveDomain(entity).Split('.', '_', '-').Select(FirstToUpper)));
            }

            static string FirstToUpper(string input)
            {
                return input.First().ToString().ToUpper() + input.Substring(1);
            }

            static string EnsureStartsWithValidCharacter(string input) => char.IsDigit(input[0]) ? "_" + input : input;

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

                // Special case for the 'all' entity. This should support all features.
                if (intValue == -1)
                {
                    foreach (var value in Enum.GetValues(supportedFeaturesType).Cast<int>())
                    {
                        yield return GetEnumValueName(value);
                    }
                    yield break;
                }
                
                if (intValue < 1)
                    yield break;

                foreach (var value in Enum.GetValues(supportedFeaturesType).Cast<int>())
                {
                    if ((intValue & value) == value)
                        yield return GetEnumValueName(value);
                }

                string GetEnumValueName(object value) =>
                    $"{GetFullAccessName(supportedFeaturesType)}.{Enum.GetName(supportedFeaturesType, value)}";
            }

            static string GetFullAccessName(Type t)
            {
                return t.DeclaringType is null ? t.GetFullName() : $"{GetFullAccessName(t.DeclaringType)}.{t.Name}";
            }
        }
    }
}