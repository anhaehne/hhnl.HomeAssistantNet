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

        IReadOnlyCollection<AutomationEntry> GetAutomationsDependingOn(string entity);
    }

    public class AutomationRegistry : IAutomationRegistry
    {
        private readonly Dictionary<string, List<AutomationEntry>> _dictionary = new();

        public AutomationRegistry(IGeneratedMetaData generatedMetaData)
        {
            Automations = generatedMetaData.AutomationMetaData.Select(x => new AutomationEntry(x))
                .ToDictionary(x => x.Info.Name);

            foreach (var automation in Automations.Values)
            foreach (var entity in automation.Info.DependsOnEntities)
            {
                var entityId = GetEntityId(entity);

                if (!_dictionary.TryGetValue(entityId, out var automationsDependingOnEntity))
                {
                    automationsDependingOnEntity = new List<AutomationEntry>();
                    _dictionary.Add(entityId, automationsDependingOnEntity);
                }

                automationsDependingOnEntity.Add(automation);
            }

            RelevantEntities = _dictionary.Keys.ToHashSet();
        }

        public IReadOnlyDictionary<string, AutomationEntry> Automations { get; }

        public ISet<string> RelevantEntities { get; }

        public IReadOnlyCollection<AutomationEntry> GetAutomationsDependingOn(string entity)
        {
            return _dictionary.TryGetValue(entity, out var automationsDependingOnEntity)
                ? automationsDependingOnEntity
                : Array.Empty<AutomationEntry>();
        }

        private string GetEntityId(Type t)
        {
            return t.GetCustomAttribute<UniqueIdAttribute>().Value;
        }
    }
}