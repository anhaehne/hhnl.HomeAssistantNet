using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public interface IAutomationRegistry
    {
        IReadOnlyCollection<AutomationRunInfo> Automations { get; }

        ISet<string> RelevantEntities { get; }

        IReadOnlyCollection<AutomationRunInfo> GetAutomationsDependingOn(string entity);
    }

    public class AutomationRegistry : IAutomationRegistry
    {
        private readonly Dictionary<string, List<AutomationRunInfo>> _dictionary = new();

        public AutomationRegistry(IGeneratedMetaData generatedMetaData)
        {
            Automations = generatedMetaData.AutomationMetaData.Select(CreateRunInfo).ToList();

            foreach (var automation in Automations)
            foreach (var entity in automation.Info.DependsOnEntities)
            {
                var entityId = GetEntityId(entity);
                
                if (!_dictionary.TryGetValue(entityId, out var automationsDependingOnEntity))
                {
                    automationsDependingOnEntity = new List<AutomationRunInfo>();
                    _dictionary.Add(entityId, automationsDependingOnEntity);
                }

                automationsDependingOnEntity.Add(automation);
            }

            RelevantEntities = _dictionary.Keys.ToHashSet();
        }

        public IReadOnlyCollection<AutomationRunInfo> Automations { get; }

        public ISet<string> RelevantEntities { get; }

        public IReadOnlyCollection<AutomationRunInfo> GetAutomationsDependingOn(string entity)
        {
            return _dictionary.TryGetValue(entity, out var automationsDependingOnEntity)
                ? automationsDependingOnEntity
                : Array.Empty<AutomationRunInfo>();
        }

        private string GetEntityId(Type t)
        {
            return t.GetCustomAttribute<UniqueIdAttribute>().Value;
        }

        private AutomationRunInfo CreateRunInfo(AutomationInfo info)
        {
            return new AutomationRunInfo
            {
                Info = info
            };
        }
    }
}