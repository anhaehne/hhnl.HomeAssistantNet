using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HADotNet.Core.Models;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        public IReadOnlyCollection<(string Code, string FullName, string ContainingClass, StateObject Entity, bool Success)> CreateEntityClasses(
            string className,
            Type entityBaseClass,
            Type? supportedFeaturesType,
            IEnumerable<StateObject> entities,
            bool removeDomain = true,
            bool supportsAllEntity = false)
        {
            var generatedClassNames = new HashSet<string>();

            if (supportsAllEntity)
                entities = entities.Append(_allEntity);

            return entities.Select(entity =>
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

                var entityClassName = ToUniqueClassName(entity.EntityId);

                var currentEntityBaseClassName = entityBaseClass.GetFullName();
                var genericTypeSubClass = string.Empty;

                // Check for open generics
                if(entityBaseClass.IsGenericType && entityBaseClass.GetGenericTypeDefinition() == entityBaseClass)
                {
                    if (entityBaseClass.GetGenericArguments().Length != 1)
                        throw new InvalidOperationException($"Only one generic parameter is allowed. Type '{currentEntityBaseClassName}'.");

                    string subClassName;
                    bool success;
                    (subClassName, genericTypeSubClass, success) = GenerateGenericType(entityBaseClass, entityClassName, entity);

                    if (!success)
                        return (string.Empty, string.Empty, string.Empty, entity, false);

                    currentEntityBaseClassName = currentEntityBaseClassName.Replace("`1", $"<{subClassName}>");
                }

                return ($@"
        /// <summary>
        /// The entity {entityName}
        /// </summary>
        [{typeof(UniqueIdAttribute).GetFullName()}(""{entity.EntityId}"")]
        [{typeof(SupportedFeaturesAttribute).GetFullName()}({supportedFeatures})]
        public class {entityClassName}: {currentEntityBaseClassName}
        {{
            public {entityClassName}({typeof(IHomeAssistantClient)} client) : base(""{entity.EntityId}"", client)
            {{
            }}

            {genericTypeSubClass}
        }}", $"{EntityNamespace}.{className}.{entityClassName}", className, entity, true);
            }).ToList();

            string ToUniqueClassName(string entity)
            {
                var counter = 0;
                var className = ToClassName(entity);

                while(generatedClassNames!.Contains(className))
                {
                    counter++;
                    className = ToClassName(entity + "-" + counter);
                }

                generatedClassNames.Add(className);
                return className;
            }

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

            static (string GenericTypeName, string GenericTypeCode, bool Success) GenerateGenericType(Type entityBaseClass, string parentName, StateObject stateObject)
            {
                var genericTypeClassGenerator = (GenericTypeClassGeneratorAttribute)entityBaseClass.GetCustomAttribute(typeof(GenericTypeClassGeneratorAttribute), false);

                // convert the object state back to json so we can bass the system.text.json.jsonelemnt
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(stateObject, new JsonSerializerSettings { ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
                });
                var jsonState = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});

                return genericTypeClassGenerator.GenerateGenericType(parentName, new EntityPoco(stateObject.EntityId) {  CurrentState = jsonState });
            }
        }

        public void WriteSourceFiles(IEnumerable<(string Code, string FullName, string ContainingClass)> classes)
        {
            foreach(var containgClass in classes.GroupBy(x => x.ContainingClass))
            {

                var source = @$"
using System;
namespace {EntityNamespace}
{{
    public static class {containgClass.Key}
    {{
        {string.Join(Environment.NewLine, containgClass.Select(x => x.Code))}
    }}
}}";

                _context.AddSource($"HomeAssistant_{containgClass.Key}", source);
            }
        }
    }
}