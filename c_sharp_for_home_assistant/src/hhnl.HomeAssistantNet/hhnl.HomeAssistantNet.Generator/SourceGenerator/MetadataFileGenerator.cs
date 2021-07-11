using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public class MetadataFileGenerator
    {
        private static readonly string _eventAnyFullAccessName = typeof(Events.Any).ToString().Replace("+", "."); 
        private static readonly string _eventCurrentFullAccessName = typeof(Events.Current).ToString().Replace("+", ".");

        private readonly GeneratorExecutionContext _context;

        public MetadataFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void AddAutomationClassMetaData(
            IReadOnlyCollection<IMethodSymbol> automationMethods,
            IReadOnlyCollection<string> entitiesFullNames)
        {
            // Add the Event types to the list so they get tracked.
            entitiesFullNames = entitiesFullNames.Concat(new[] { _eventAnyFullAccessName, _eventCurrentFullAccessName }).ToList();

            var registerEntities = string.Join(Environment.NewLine,
                entitiesFullNames.Select(t =>
                    $"            serviceCollection.AddSingleton<{t}>();"));

            var entityTypes = string.Join(Environment.NewLine,
                entitiesFullNames.Select(t => $"            typeof({t}),"));


            var sourceText = $@"
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable CS1998

namespace {nameof(hhnl)}.{nameof(HomeAssistantNet)}.Generated
{{
    public class {GeneratorConstants.MetaDataClassName} : {typeof(IGeneratedMetaData).GetFullName()} 
    {{
        public void {nameof(IGeneratedMetaData.RegisterEntitiesAndAutomations)}(IServiceCollection serviceCollection)
        {{
{registerEntities} 
        }}

        public IReadOnlyCollection<Type> {nameof(IGeneratedMetaData.EntityTypes)} {{ get; }} = new Type[]
        {{
{entityTypes}
        }};
    }}
}}";

            _context.AddSource("GeneratedMetaData.cs", sourceText);
        }
    }
}