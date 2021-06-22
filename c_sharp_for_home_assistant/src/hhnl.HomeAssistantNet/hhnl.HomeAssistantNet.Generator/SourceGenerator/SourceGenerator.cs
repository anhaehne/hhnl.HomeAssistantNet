using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HADotNet.Core;
using HADotNet.Core.Clients;
using HADotNet.Core.Domain;
using hhnl.HomeAssistantNet.Generator.Configuration;
using hhnl.HomeAssistantNet.Shared.Entities;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutomationClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            // if (!Debugger.IsAttached)
            // {
            //     Debugger.Launch();
            // }
#endif

            if (!HomeAssistantCompileTimeConfigReader.TryGetConfig(context.AdditionalFiles,
                out var config,
                out var diagnostic,
                context.CancellationToken))
            {
                context.ReportDiagnostic(diagnostic);
                return;
            }

            try
            {
                ClientFactory.Initialize(config.Instance, config.Token);
                Task.Run(() => Run(context)).GetAwaiter().GetResult();
            }
            catch (HttpResponseException e)
            {
                var message =
                    $"Unable to connect to HomeAssistant api. HomeAssistance url: '{config.Instance}' Token: '{config.Token.Substring(0, 10)}...' StatusCode: '{e.StatusCode}' RequestPath '{e.RequestPath}' NetworkDescription: '{e.NetworkDescription}' Message '{e.Message}' InnerException: '{e.InnerException}' StackTrace: {e.StackTrace}";
                var dd = new DiagnosticDescriptor("HHNL005", message, message, "Error", DiagnosticSeverity.Error, true);
                context.ReportDiagnostic(Diagnostic.Create(dd, Location.None));
            }
            catch (Exception e)
            {
                var message =
                    $"An unhandled exception occured '{e.GetType()}'. HomeAssistance url: '{config.Instance}' Token: '{config.Token.Substring(0, 10)}...'  Message '{e.Message}' InnerException: '{e.InnerException}' StackTrace: {e.StackTrace}";
                var dd = new DiagnosticDescriptor("HHNL005", message, message, "Error", DiagnosticSeverity.Error, true);
                context.ReportDiagnostic(Diagnostic.Create(dd, Location.None));
            }
        }

        private async Task Run(GeneratorExecutionContext context)
        {
            var entityFullNames = await GenerateEntityClasses(context);

            GenerateAutomationMetaData(context, entityFullNames);
        }

        private static void GenerateAutomationMetaData(
            GeneratorExecutionContext context,
            IReadOnlyCollection<string> entitiesFullNames)
        {
            var receiver = (AutomationClassSyntaxReceiver)context.SyntaxContextReceiver!;

            var metaDataClassGenerator = new MetadataFileGenerator(context);
            metaDataClassGenerator.AddAutomationClassMetaData(receiver.AutomationMethods, entitiesFullNames);
        }

        private static async Task<IReadOnlyCollection<string>> GenerateEntityClasses(GeneratorExecutionContext context)
        {
            var entityClassGenerator = new EntityClassSourceFileGenerator(context);

            var entityClient = ClientFactory.GetClient<StatesClient>();
            var entities = (await entityClient.GetStates()).ToDictionary(x => x.EntityId);

            List<string> entitiesFullNames = new();

            // Get the type of entities with a HomeAssistantEntityAttribute and load their meta data.
            var knownEntityDomains = typeof(Entity).Assembly.GetTypes().Where(t => typeof(Entity).IsAssignableFrom(t))
                .Select(t => (Type: t, Attribute: t.GetCustomAttribute<HomeAssistantEntityAttribute>()))
                .Where(t => t.Attribute is not null)
                .ToDictionary(t => t.Attribute.Domain,
                    t => (t.Attribute.ContainingEntityClass, EntityBaseClass: t.Type,
                        t.Attribute.SupportedFeaturesEnumType, t.Attribute.SupportsAllEntity));

            // Group entities by domain and generate their classes.
            foreach (var knownEntityDomain in knownEntityDomains)
            {
                var entitiesOfDomain =
                    entities.Where(x => x.Key.StartsWith(knownEntityDomain.Key)).Select(x => x.Value).ToList();

                var typedFullNames = entityClassGenerator.AddEntityClass(knownEntityDomain.Value.ContainingEntityClass,
                    knownEntityDomain.Value.EntityBaseClass,
                    knownEntityDomain.Value.SupportedFeaturesEnumType,
                    entitiesOfDomain,
                    supportsAllEntity: knownEntityDomain.Value.SupportsAllEntity);

                entitiesFullNames.AddRange(typedFullNames);

                foreach (var e in entitiesOfDomain)
                {
                    entities.Remove(e.EntityId);
                }
            }

            // Generate all unknown entities inside the "Entities" class.
            var fullNames = entityClassGenerator.AddEntityClass("Entities", typeof(Entity), null, entities.Values, false);
            entitiesFullNames.AddRange(fullNames);

            return entitiesFullNames;
        }
    }
}