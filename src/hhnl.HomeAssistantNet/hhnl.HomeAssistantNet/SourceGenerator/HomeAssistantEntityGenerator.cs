using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HADotNet.Core;
using HADotNet.Core.Clients;
using hhnl.HomeAssistantNet.Configuration;
using hhnl.HomeAssistantNet.Entities;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.SourceGenerator
{
    [Generator]
    public class HomeAssistantEntityGenerator : ISourceGenerator
    {
        private static readonly Dictionary<string, (string staticClass, string entityClass, Type? supportedFeatures)> _knownEntityDomains =  new Dictionary<string, (string staticClass, string entityClass, Type? supportedFeatures)>()
        {
            {
                "light",
                ("Lights", "Light", typeof(Light.SupportedFeatures))
            }
        };

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AutomationClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif

            if (!HomeAssistantConfigReader.TryGetConfig(context.AdditionalFiles,
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
            GenerateAutomationMetaData(context);
            await GenerateEntityClasses(context);
        }

        private static void GenerateAutomationMetaData(GeneratorExecutionContext context)
        {
            var receiver = (AutomationClassSyntaxReceiver)context.SyntaxContextReceiver!;
            
            var metaDataClassGenerator = new AutomationClassMetadataFileGenerator(context);
            metaDataClassGenerator.AddAutomationClassMetaData(receiver.AutomationMethods);
        }
        
        private static async Task GenerateEntityClasses(GeneratorExecutionContext context)
        {
            var entityClassGenerator = new EntityClassSourceFileGenerator(context);

            var entityClient = ClientFactory.GetClient<StatesClient>();
            var entities = (await entityClient.GetStates()).ToDictionary(x => x.EntityId);

            foreach (var knownEntityDomain in _knownEntityDomains)
            {
                var entitiesOfDomain = entities.Where(x => x.Key.StartsWith(knownEntityDomain.Key)).Select(x => x.Value).ToList();

                entityClassGenerator.AddEntityClass(knownEntityDomain.Value.staticClass,
                    knownEntityDomain.Value.entityClass,
                    knownEntityDomain.Value.supportedFeatures,
                    entitiesOfDomain);

                foreach (var e in entitiesOfDomain)
                    entities.Remove(e.EntityId);
            }

            entityClassGenerator.AddEntityClass("Entities", "Entity", null, entities.Values, false);
        }
    }
}