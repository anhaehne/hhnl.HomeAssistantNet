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

        public object GetSnapshot(Type t)
        {
            if (!AutomationRunContext.GetRunContextOrFail().CurrentRun.EntitySnapshot.ContainsKey(t))
                throw new InvalidOperationException($"Entity snapshot didn't include {t}");

            return AutomationRunContext.GetRunContextOrFail().CurrentRun.EntitySnapshot[t];
        }
    }
}
