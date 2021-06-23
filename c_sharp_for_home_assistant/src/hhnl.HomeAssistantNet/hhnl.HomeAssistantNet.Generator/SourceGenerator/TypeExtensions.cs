using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    public static class TypeExtensions
    {
        public static string GetFullName(this ITypeSymbol type, IReadOnlyCollection<string>? entities)
        {
            entities ??= Array.Empty<string>();
            
            var sb = new StringBuilder(type.MetadataName);

            ISymbol s = type.ContainingSymbol;

            while (s is not INamespaceSymbol { IsGlobalNamespace: true })
            {
                sb.Insert(0, '.');
                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                s = s.ContainingSymbol;
            }

            var fullName = sb.ToString();

            var entityFullName = $"{EntityClassSourceFileGenerator.EntityNamespace}.{fullName}";
            return entities.Contains(entityFullName) ? entityFullName : fullName;
        }
        
        public static string GetFullName(this Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }
    }
}