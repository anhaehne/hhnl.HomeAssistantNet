using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public class MetadataFileGenerator
    {
        private static readonly string _automationAttributeFullAccessName = typeof(AutomationAttribute).ToString();

        private readonly GeneratorExecutionContext _context;

        public MetadataFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void AddAutomationClassMetaData(
            IReadOnlyCollection<IMethodSymbol> automationMethods,
            IReadOnlyCollection<string> entitiesFullNames)
        {
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
                            // Allow passing the cancellation token.
                            if (p.Type.GetFullName() == typeof(CancellationToken).GetFullName())
                                return "ct";
                            
                            return $"s.GetRequiredService<{p.Type.GetFullName(entitiesFullNames)}>()";
                        }));

                    var dependingOnEntities = method.Parameters.Where(p => entitiesFullNames.Contains(p.Type.GetFullName()))
                        .Select(p => $"typeof({p.Type.GetFullName(entitiesFullNames)})");

                    return $@"            new {typeof(AutomationInfo).GetFullName()}
            {{
                {nameof(AutomationInfo.Error)} = ""{(error is null ? string.Empty : error.GetMessage())}"",
                {nameof(AutomationInfo.Method)} = typeof({method.ContainingType.GetFullName()}).GetMethod(""{method.Name}"", BindingFlags.Instance | BindingFlags.Public)!,
                {nameof(AutomationInfo.DisplayName)} = ""{attributeData.ConstructorArguments[0].Value ?? method.Name}"",
                {nameof(AutomationInfo.Name)} = ""{method.ContainingType.GetFullName()}.{method.Name}"",
                {nameof(AutomationInfo.RunOnStart)} = {(attributeData.ConstructorArguments[1].Value is true ? "true" : "false")},
                {nameof(AutomationInfo.ReentryPolicy)} = ({typeof(ReentryPolicy).GetFullName()}){attributeData.ConstructorArguments[2].Value},
                {nameof(AutomationInfo.DependsOnEntities)} = new Type[] {{ {string.Join(", ", dependingOnEntities)} }},
                {nameof(AutomationInfo.RunAutomation)} = async (s, ct) => 
                {{
                    {(method.IsAsync ? "await " : string.Empty)}s.GetRequiredService<{method.ContainingType.GetFullName()}>().{method.Name}({arguments});
                }}
            }},";
                }));

            var registerEntities = string.Join(Environment.NewLine,
                entitiesFullNames.Select(t =>
                    $"            serviceCollection.AddSingleton<{EntityClassSourceFileGenerator.EntityNamespace}.{t}>();"));

            var registerAutomationClasses = string.Join(Environment.NewLine,
                verifiedAutomationMethods.Select(m => m.Method.ContainingType).Distinct()
                    .Select(x => $"            serviceCollection.AddSingleton<{x}>();"));

            var entityTypes = string.Join(Environment.NewLine,
                entitiesFullNames.Select(t => $"            typeof({EntityClassSourceFileGenerator.EntityNamespace}.{t}),"));


            var sourceText = $@"
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    }
}