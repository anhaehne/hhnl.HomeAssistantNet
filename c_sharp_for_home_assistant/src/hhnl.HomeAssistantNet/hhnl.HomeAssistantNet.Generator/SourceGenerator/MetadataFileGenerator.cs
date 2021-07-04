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
        private static readonly string _automationAttributeFullAccessName = typeof(AutomationAttribute).ToString();
        private static readonly string _outputOnlyAttributeFullAccessName = typeof(NoTrackAttribute).ToString();
        private static readonly string _snapshotAttributeFullAccessName = typeof(SnapshotAttribute).ToString();
        private static readonly string _eventFullAccessName = typeof(Event).ToString();

        private readonly GeneratorExecutionContext _context;

        public MetadataFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void AddAutomationClassMetaData(
            IReadOnlyCollection<IMethodSymbol> automationMethods,
            IReadOnlyCollection<string> entitiesFullNames)
        {
            // Add the Event type to the list so it gets tracked.
            entitiesFullNames = entitiesFullNames.Append(_eventFullAccessName).ToList();

            var verifiedAutomationMethods =
                AutomationMethodValidator.VerifyMethodSymbols(automationMethods, _context.ReportDiagnostic)
                    .ToList();

            var automationInfo = string.Join(Environment.NewLine,
                verifiedAutomationMethods.Select(automationMethod =>
                {
                    var (method, error) = automationMethod;

                    var attributeData = method.GetAttributes()
                        .First(a => a.AttributeClass!.ToString() == _automationAttributeFullAccessName);

                    var arguments = string.Join(",",
                        method.Parameters.Select(p =>
                        {
                            var typeFullName = p.Type.GetFullName(entitiesFullNames);

                            // Allow passing the cancellation token.
                            if (typeFullName == typeof(CancellationToken).GetFullName())
                                return "ct";

                            // Request snapshot entites and the source event from the IEntitySnapshotProvider
                            if (ParameterHasSnapshotAttribute(p) || typeFullName == _eventFullAccessName)
                                return $"s.GetRequiredService<{typeof(IEntitySnapshotProvider).GetFullName()}>().{nameof(IEntitySnapshotProvider.GetSnapshot)}<{typeFullName}>()";

                            return $"s.GetRequiredService<{typeFullName}>()";
                        }));

                    var classEntityDependencies = GetClassEntityDependencies(method, entitiesFullNames);
                    var dependingOnEntities = GetMethodEntityDependencies(method, entitiesFullNames).ToList();

                    var entityDependencies = classEntityDependencies.Concat(dependingOnEntities.Select(x => x.Entity));
                    var listenToEntities = dependingOnEntities.Where(x => !x.NoTrack).Select(x => x.Entity);

                    var snapshotEntities = GetMethodSnapshotEntities(method, entitiesFullNames);

                    return $@"            new {typeof(AutomationInfo).GetFullName()}
            {{
                {nameof(AutomationInfo.GenerationError)} = ""{(error is null ? string.Empty : error.GetMessage())}"",
                {nameof(AutomationInfo.Method)} = typeof({method.ContainingType.GetFullName(entitiesFullNames)}).GetMethod(""{method.Name}"", BindingFlags.Instance | BindingFlags.Public)!,
                {nameof(AutomationInfo.DisplayName)} = ""{attributeData.ConstructorArguments[0].Value ?? method.Name}"",
                {nameof(AutomationInfo.Name)} = ""{method.ContainingType.GetFullName(entitiesFullNames)}.{method.Name}"",
                {nameof(AutomationInfo.RunOnStart)} = {(attributeData.ConstructorArguments[1].Value is true ? "true" : "false")},
                {nameof(AutomationInfo.ReentryPolicy)} = ({typeof(ReentryPolicy).GetFullName()}){attributeData.ConstructorArguments[2].Value},
                {nameof(AutomationInfo.DependsOnEntities)} = new Type[] {{ {string.Join(", ", entityDependencies)} }},
                {nameof(AutomationInfo.ListenToEntities)} = new Type[] {{ {string.Join(", ", listenToEntities)} }},
                {nameof(AutomationInfo.SnapshotEntities)} = new Type[] {{ {string.Join(", ", snapshotEntities)} }},
                {nameof(AutomationInfo.RunAutomation)} = async (s, ct) => 
                {{
                    {(method.IsAsync ? "await " : string.Empty)}s.GetRequiredService<{method.ContainingType.GetFullName(entitiesFullNames)}>().{method.Name}({arguments});
                }}
            }},";
                }));

            var registerEntities = string.Join(Environment.NewLine,
                entitiesFullNames.Select(t =>
                    $"            serviceCollection.AddSingleton<{t}>();"));

            var registerAutomationClasses = string.Join(Environment.NewLine,
                verifiedAutomationMethods.Select(m => m.Method.ContainingType).Distinct()
                    .Select(x => $"            serviceCollection.AddSingleton<{x}>();"));

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
{registerAutomationClasses}       
        }}

        public IReadOnlyCollection<{typeof(AutomationInfo).GetFullName()}> {nameof(IGeneratedMetaData.AutomationMetaData)} {{ get; }} = new {typeof(AutomationInfo).GetFullName()}[]
        {{
{automationInfo}
        }};

        public IReadOnlyCollection<Type> {nameof(IGeneratedMetaData.EntityTypes)} {{ get; }} = new Type[]
        {{
{entityTypes}
        }};
    }}
}}";

            _context.AddSource("GeneratedMetaData.cs", sourceText);
        }

        private static IEnumerable<string> GetClassEntityDependencies(
            IMethodSymbol methodSymbol,
            IReadOnlyCollection<string> entitiesFullNames)
        {
            return methodSymbol.ContainingType.Constructors.SelectMany(c => c.Parameters
                .Where(p => entitiesFullNames.Contains(p.Type.GetFullName(entitiesFullNames)))
                .Select(p => $"typeof({p.Type.GetFullName(entitiesFullNames)})"));
        }

        private static IEnumerable<(string Entity, bool NoTrack)> GetMethodEntityDependencies(
            IMethodSymbol methodSymbol,
            IReadOnlyCollection<string> entitiesFullNames)
        {
            return methodSymbol.Parameters
                .Where(p => entitiesFullNames.Contains(p.Type.GetFullName(entitiesFullNames)))
                .Select(p => ($"typeof({p.Type.GetFullName(entitiesFullNames)})",
                    p.GetAttributes().Any(a => a.AttributeClass!.ToString() == _outputOnlyAttributeFullAccessName)));
        }

        private static IEnumerable<string> GetMethodSnapshotEntities(
            IMethodSymbol methodSymbol,
            IReadOnlyCollection<string> entitiesFullNames) => methodSymbol.Parameters
                .Where(p =>
                {
                    var fullName = p.Type.GetFullName(entitiesFullNames);

                    // If the soure event is requested, we include it in the snapshot
                    if (fullName == _eventFullAccessName)
                        return true;

                    return entitiesFullNames.Contains(fullName) && ParameterHasSnapshotAttribute(p);
                })
                .Select(p => $"typeof({p.Type.GetFullName(entitiesFullNames)})");

        private static bool ParameterHasSnapshotAttribute(IParameterSymbol p) => p.GetAttributes().Any(a => a.AttributeClass!.ToString() == _snapshotAttributeFullAccessName);
    }
}