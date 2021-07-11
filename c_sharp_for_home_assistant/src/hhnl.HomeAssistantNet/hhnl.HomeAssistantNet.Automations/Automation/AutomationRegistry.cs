using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRegistry
    {
        IReadOnlyDictionary<string, AutomationEntry> Automations { get; }

        ISet<string> RelevantEntities { get; }

        IReadOnlyCollection<AutomationEntry> GetAutomationsTrackingEntity(string entity);

        public bool HasAutomationsTrackingAnyEvents { get;}
    }

    public class AutomationRegistry : IAutomationRegistry
    {
        private readonly Dictionary<string, List<AutomationEntry>> _automationsListeningToEntities = new();

        public AutomationRegistry(IGeneratedMetaData generatedMetaData, IAutomationInfoProvider automationInfoProvider, Assembly assembly)
        {
            Automations = automationInfoProvider.DiscoverAutomations(assembly.GetTypes()).Select(x => new AutomationEntry(x))
                .ToDictionary(x => x.Info.Name);

            foreach (var automation in Automations.Values)
            foreach (var entity in automation.Info.ListenToEntities)
            {
                var entityId = GetEntityId(entity);

                if (!_automationsListeningToEntities.TryGetValue(entityId, out var automationsListeningToEntity))
                {
                    automationsListeningToEntity = new List<AutomationEntry>();
                    _automationsListeningToEntities.Add(entityId, automationsListeningToEntity);
                }

                automationsListeningToEntity.Add(automation);
            }

            RelevantEntities = Automations.SelectMany(a => a.Value.Info.DependsOnEntities).Select(GetEntityId).Where(x => x != Events.Current.UniqueId && x != Events.Any.UniqueId).ToHashSet();

            HasAutomationsTrackingAnyEvents = Automations.Any(a => a.Value.Info.ListenToEntities.Contains(typeof(Events.Any)));
        }

        public IReadOnlyDictionary<string, AutomationEntry> Automations { get; }

        public ISet<string> RelevantEntities { get; }

        public bool HasAutomationsTrackingAnyEvents { get; }

        public IReadOnlyCollection<AutomationEntry> GetAutomationsTrackingEntity(string entity)
        {
            return _automationsListeningToEntities.TryGetValue(entity, out var automationsDependingOnEntity)
                ? automationsDependingOnEntity
                : Array.Empty<AutomationEntry>();
        }

        private static string GetEntityId(Type t)
        {
            var uniqueIdAttribute = t.GetCustomAttribute<UniqueIdAttribute>();

            if (uniqueIdAttribute is null)
                throw new ArgumentException($"Type '{t}' is not an entity.");
            
            return uniqueIdAttribute.Value;
        }
    }
}