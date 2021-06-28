using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRegistry
    {
        IReadOnlyDictionary<string, AutomationEntry> Automations { get; }

        ISet<string> RelevantEntities { get; }

        IReadOnlyCollection<AutomationEntry> GetAutomationsTrackingEntity(string entity);
    }

    public class AutomationRegistry : IAutomationRegistry
    {
        private readonly Dictionary<string, List<AutomationEntry>> _automationsListeningToEntities = new();

        public AutomationRegistry(IGeneratedMetaData generatedMetaData)
        {
            Automations = generatedMetaData.AutomationMetaData.Select(x => new AutomationEntry(x))
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

            RelevantEntities = _automationsListeningToEntities.Keys.ToHashSet();
        }

        public IReadOnlyDictionary<string, AutomationEntry> Automations { get; }

        public ISet<string> RelevantEntities { get; }

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