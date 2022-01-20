using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public static class AutomationMethodValidator
    {
        private static readonly DiagnosticDescriptor _invalidAutomationDeclaration = new("HHNLHAN003",
            "Automation method is invalid",
            "{0} The automation will be ignored",
            "Setup",
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor _invalidAutomationClassDeclaration = new("HHNLHAN004",
            "Class containing automation methods is invalid",
            "{0} All automations will be ignored",
            "Setup",
            DiagnosticSeverity.Warning,
            true);

        
         public static IEnumerable<(IMethodSymbol Method, Diagnostic? Error)> VerifyMethodSymbols(
            IReadOnlyCollection<IMethodSymbol> automationMethods,
            Action<Diagnostic> reportFunc)
        {
            var groupedByType = automationMethods.GroupBy<IMethodSymbol, INamedTypeSymbol>(x => x.ContainingType, SymbolEqualityComparer.Default);

            foreach (var group in groupedByType)
            {
                // Validate class
                var classError = VerifyTypeDeclaration(group.Key);

                if (classError is not null)
                {
                    reportFunc(classError);

                    foreach (var method in group)
                        yield return (method, classError);
                    
                    yield break;
                }

                // Make sure no duplicate methods exists
                var results = VerifyDuplicateMethodNames(group, reportFunc);

                // Validate methods
                foreach (var result in results)
                {
                    var (method, error) = result;
                    error ??= VerifyMethodDeclaration(method);

                    if (error is not null)
                        reportFunc(error);

                    yield return (method, error);
                }
            }
        }

        // TODO: Move diagnostics to separate analyzer.
        private static Diagnostic? VerifyTypeDeclaration(INamedTypeSymbol type)
        {
            if (type.IsStatic)
            {
                return Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods can't be static.");
            }

            if (type.IsAbstract)
            {
                // We just ignore abstract classes to allow for base classes if needed.
                return null;
            }

            if (type.IsGenericType)
            {
                return Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods can't be generic.");
            }

            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                return Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods must be public.");
            }

            return null;
        }

        private static IEnumerable<(IMethodSymbol Method, Diagnostic? Error)> VerifyDuplicateMethodNames(
            IEnumerable<IMethodSymbol> methods,
            Action<Diagnostic> reportFunc)
        {
            var groups = methods.GroupBy(m => m, SymbolEqualityComparer.Default);

            foreach (var group in groups)
            {
                if (group.Count() == 1)
                {
                    foreach (var method in group)
                    {
                        yield return (method, null);
                    }
                }
                else
                {
                    foreach (var method in group)
                    {
                        var error = Diagnostic.Create(_invalidAutomationDeclaration,
                            method.Locations.First(),
                            DiagnosticSeverity.Warning,
                            "Automation method names must be unique.");
                        reportFunc(error);
                        yield return (method, error);
                    }
                }
            }
        }

        private static Diagnostic? VerifyMethodDeclaration(IMethodSymbol method)
        {
            if (method.IsStatic)
            {
                return Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be static.");
            }

            if (method.IsGenericMethod)
            {
                return Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be generic.");
            }

            if (method.IsAbstract)
            {
                return Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be abstract.");
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                return Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods must be public.");
            }

            return null;
        }
    }
}