using System.Collections.Generic;
using System.Linq;
using hhnl.HomeAssistantNet.Shared.Automation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public class AutomationClassSyntaxReceiver : ISyntaxContextReceiver
    {
        private readonly List<IMethodSymbol> _automationMethods = new();

        public IReadOnlyCollection<IMethodSymbol> AutomationMethods => _automationMethods;

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not MethodDeclarationSyntax methodDeclaration)
                return;

            CheckIfIsAutomationMethod(context, methodDeclaration);
        }

        private void CheckIfIsAutomationMethod(GeneratorSyntaxContext context, MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration.AttributeLists.Count == 0)
                return;

            if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == typeof(AutomationAttribute).FullName))
                _automationMethods.Add(methodSymbol);
        }
    }
}