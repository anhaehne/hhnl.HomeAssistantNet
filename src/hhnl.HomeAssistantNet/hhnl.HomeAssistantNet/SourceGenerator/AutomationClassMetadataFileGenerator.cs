using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hhnl.HomeAssistantNet.Automation;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.SourceGenerator
{
    public class AutomationClassMetadataFileGenerator
    {
        private const string Source1 = @"
using System.Reflection;
using ";

        private const string Source2 = @";

namespace ";

        private const string Source3 = @"
{
    public class AutomationMetaData_Generated: "; 

        private const string Source4 = @"
    {
        public AutomationMetaData_Generated() : base(
            new []
            {
";

        private const string Source5 = @"
            })
        {
        }
    }
}";

        private static readonly string _automationAttributeFullAccessName = typeof(AutomationAttribute).ToString();

        private readonly GeneratorExecutionContext _context;

        public AutomationClassMetadataFileGenerator(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void AddAutomationClassMetaData(IReadOnlyCollection<IMethodSymbol> automationMethods)
        {
            var validMethods =
                AutomationMethodValidator.VerifyAndFilterMethodSymbols(automationMethods, _context.ReportDiagnostic)
                    .ToList();

            if (!validMethods.Any())
                return;
            
            var source = new StringBuilder(Source1);
            source.Append(typeof(AutomationMetaData).Namespace);
            source.Append(Source2);
            source.Append(validMethods.First().ContainingAssembly.Identity.Name);
            source.Append(Source3);
            source.Append(nameof(AutomationMetaData));
            source.Append(Source4);

            foreach (var method in validMethods)
            {
                var attributeData = method.GetAttributes().First(a => a.AttributeClass!.ToString() == _automationAttributeFullAccessName);

                source.AppendLine("                new AutomationInfo");
                source.AppendLine("                {");

                source.AppendLine(
                    $"                    Method = typeof({method.ContainingType}).GetMethod(\"{method.Name}\", BindingFlags.Instance | BindingFlags.Public),");

                source.AppendLine(
                    $"                    DisplayName = \"{attributeData.ConstructorArguments[0].Value ?? method.Name}\",");
                source.AppendLine($"                    RunOnStart = {(attributeData.ConstructorArguments[1].Value is true ? "true": "false")}");
                source.AppendLine("                },");
            }

            source.AppendLine(Source5);

            var sourceText = source.ToString();
            
            _context.AddSource("AutomationMetaData_Generated.cs", sourceText);
        }
    }
}