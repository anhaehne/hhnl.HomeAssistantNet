using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.SourceGenerator
{
    public static class AutomationMethodValidator
    {
        private static readonly DiagnosticDescriptor _invalidAutomationDeclaration = new("HHNLHAN003",
            "Automation method is invalid.",
            "{0} The automation will be ignored.",
            "Setup",
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor _invalidAutomationClassDeclaration = new("HHNLHAN004",
            "Class containing automation methods is invalid.",
            "{0} All automations will be ignored.",
            "Setup",
            DiagnosticSeverity.Warning,
            true);

        
         public static IEnumerable<IMethodSymbol> VerifyAndFilterMethodSymbols(
            IReadOnlyCollection<IMethodSymbol> automationMethods,
            Action<Diagnostic> reportFunc)
        {
            var groupedByType = automationMethods.GroupBy(x => x.ContainingType);

            foreach (var group in groupedByType)
            {
                if (!VerifyTypeDeclaration(group.Key, reportFunc))
                    continue;

                var uniqueMethods = VerifyAndFilterDuplicateMethodNames(group, reportFunc);
                var validMethods = uniqueMethods.Where(m => VerifyMethodDeclaration(m, reportFunc)).ToList();

                if (!validMethods.Any())
                    continue;

                foreach (var validMethod in validMethods)
                {
                    yield return validMethod;
                }
            }
        }

        // TODO: Move diagnostics to separate analyzer.
        private static bool VerifyTypeDeclaration(INamedTypeSymbol type, Action<Diagnostic> reportFunc)
        {
            if (type.IsStatic)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods can't be static."));
                return false;
            }

            if (type.IsGenericType)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods can't be generic."));
                return false;
            }

            if (type.IsAbstract)
            {
                // We just ignore abstract classes to allow for base classes if needed.
                return false;
            }

            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationClassDeclaration,
                    type.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Classes containing automation methods must be public."));
                return false;
            }

            return true;
        }

        private static IEnumerable<IMethodSymbol> VerifyAndFilterDuplicateMethodNames(
            IEnumerable<IMethodSymbol> methods,
            Action<Diagnostic> reportFunc)
        {
            var groups = methods.GroupBy(m => m.Name);

            foreach (var group in groups)
            {
                if (group.Count() == 1)
                {
                    foreach (var method in group)
                    {
                        yield return method;
                    }
                }
                else
                {
                    foreach (var method in group)
                    {
                        reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                            method.Locations.First(),
                            DiagnosticSeverity.Warning,
                            "Automation method names must be unique."));
                    }
                }
            }
        }

        private static bool VerifyMethodDeclaration(IMethodSymbol method, Action<Diagnostic> reportFunc)
        {
            if (method.IsStatic)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be static."));
                return false;
            }

            if (method.IsGenericMethod)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be generic."));
                return false;
            }

            if (method.IsAbstract)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can't be abstract."));
                return false;
            }

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods must be public."));
                return false;
            }

            if (method.Parameters.Any())
            {
                reportFunc(Diagnostic.Create(_invalidAutomationDeclaration,
                    method.Locations.First(),
                    DiagnosticSeverity.Warning,
                    "Automation methods can not contain parameters."));
                return false;
            }

            return true;
        }
    }
}