using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HADotNet.Core;
using HADotNet.Core.Clients;
using hhnl.HomeAssistantNet.Generator.Configuration;
using hhnl.HomeAssistantNet.Shared.Entities;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.SourceGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        private static readonly Dictionary<string, (string staticClass, Type EntityBaseClass, Type? supportedFeatures)> _knownEntityDomains =  new()
        {
            {
                "light",
                ("Lights", typeof(Light), typeof(Light.SupportedFeatures))
            }
        };

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

            ClientFactory.Initialize(config.Instance, config.Token);

            Task.Run(() => Run(context)).GetAwaiter().GetResult();

            Debug.WriteLine("Execute code generator");
        }

        private async Task Run(GeneratorExecutionContext context)
        {
            var entityFullNames = await GenerateEntityClasses(context);
            
            GenerateAutomationMetaData(context, entityFullNames);
        }

        private static void GenerateAutomationMetaData(GeneratorExecutionContext context, IReadOnlyCollection<string> entitiesFullNames)
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

            foreach (var knownEntityDomain in _knownEntityDomains)
            {
                var entitiesOfDomain = entities.Where(x => x.Key.StartsWith(knownEntityDomain.Key)).Select(x => x.Value).ToList();

                var typedFullNames = entityClassGenerator.AddEntityClass(knownEntityDomain.Value.staticClass,
                    knownEntityDomain.Value.EntityBaseClass,
                    knownEntityDomain.Value.supportedFeatures,
                    entitiesOfDomain);
                
                entitiesFullNames.AddRange(typedFullNames);

                foreach (var e in entitiesOfDomain)
                    entities.Remove(e.EntityId);
            }

            var fullNames = entityClassGenerator.AddEntityClass("Entities", typeof(Entity), null, entities.Values, false);
            entitiesFullNames.AddRange(fullNames);
            
            return entitiesFullNames;
        }
    }
}