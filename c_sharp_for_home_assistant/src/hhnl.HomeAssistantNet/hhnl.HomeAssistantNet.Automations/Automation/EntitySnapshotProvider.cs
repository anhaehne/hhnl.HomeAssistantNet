using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using System;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class EntitySnapshotProvider : IEntitySnapshotProvider
    {
        public T GetSnapshot<T>()
        {
            if (!AutomationRunContext.GetRunContextOrFail().CurrentRun.EntitySnapshot.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"Entity snapshot didn't include {typeof(T)}");

            return (T)AutomationRunContext.GetRunContextOrFail().CurrentRun.EntitySnapshot[typeof(T)];
        }
    }
}
