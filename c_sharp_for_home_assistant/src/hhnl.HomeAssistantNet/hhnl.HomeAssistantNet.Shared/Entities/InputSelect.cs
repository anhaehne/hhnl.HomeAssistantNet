using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using hhnl.HomeAssistantNet.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Entities
{
    [HomeAssistantEntity("input_select", "InputSelects")]
    [InputSelectGenericTypeClassGenerator]
    public abstract class InputSelect<TValue> : Entity where TValue : Enum
    {
        private static readonly IReadOnlyDictionary<string, TValue> _mapping = GetValueMapping();
        private static readonly IReadOnlyDictionary<TValue, string> _reverseMapping = _mapping.ToDictionary(x => x.Value, x => x.Key);

        public InputSelect(string uniqueId, IHomeAssistantClient assistantClient) : base(uniqueId, assistantClient)
        {
        }

        public bool ValueIsUnknown => State is null || State?.ToString() == "unknown";

        /// <summary>
        /// Gets the current value of the entity.
        /// </summary>
        /// <returns>The value or <c>null</c> if the value is unkown.</returns>
        public TValue? GetValue()
        {
            if (ValueIsUnknown)
                return default;

            return Parse(State!);
        }

        public Task SelectOptionAsync(TValue value, CancellationToken cancellationToken = default)
        {
            return HomeAssistantClient.CallServiceAsync("input_select", "select_option", UniqueId, new
            {
                option = _reverseMapping[value],
            }, cancellationToken);
        }

        protected virtual TValue? Parse(string state)
        {
            if(!_mapping.TryGetValue(state, out var value))
                throw new StateInvalidException($"Unable to convert the state of entity '{UniqueId}' ('{state?.ToString()}') to type '{typeof(TValue)}'.", null);

            return value;
        }

        private static IReadOnlyDictionary<string, TValue> GetValueMapping()
        {
            return Enum.GetValues(typeof(TValue)).Cast<TValue>().ToDictionary(GetName);

            static string GetName(TValue value)
            {
                var member = typeof(TValue).GetMember(Enum.GetName(typeof(TValue), value)).SingleOrDefault() ?? throw new InvalidOperationException($"Unable to find member '{value}' for enum '{typeof(TValue)}'.");
                var memerNameAttribute = (EnumMemberNameAttribute)member.GetCustomAttributes(typeof(EnumMemberNameAttribute), false).SingleOrDefault() ?? throw new InvalidOperationException($"Unable to find member name for the value'{value}' of the enum '{typeof(TValue)}'.");
                return memerNameAttribute.Name;
            }
        }
    }

    public class InputSelectGenericTypeClassGeneratorAttribute : GenericTypeClassGeneratorAttribute
    {
        public override (string GenericTypeName, string GenericTypeCode, bool Success) GenerateGenericType(string parentName, EntityPoco entity)
        {
            var options = entity.GetAttributeOrDefault<string[]>("options");

            if (options is null)
                return (string.Empty, string.Empty, false);

            var source = @$"public enum Options 
            {{
{string.Join(Environment.NewLine, options.Select(GetOption))}
            }}";

            static string GetOption(string option)
            {
                return @$"                /// {option}
                [hhnl.HomeAssistantNet.Shared.Entities.EnumMemberNameAttribute(""{option}"")]
                {EscapeOption(option)},

";
            }

            static string EscapeOption(string option)
            {
                var withoutSpaces = string.Join("", option.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
                return Regex.Replace(withoutSpaces, "[^a-zA-Z0-9_]", "");
            }

            return (parentName + ".Options", source, true);
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EnumMemberNameAttribute : Attribute
    {
        public EnumMemberNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
