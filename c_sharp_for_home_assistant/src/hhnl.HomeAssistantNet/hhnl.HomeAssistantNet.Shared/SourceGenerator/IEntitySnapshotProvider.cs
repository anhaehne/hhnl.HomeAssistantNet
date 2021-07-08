using System;

namespace hhnl.HomeAssistantNet.Shared.SourceGenerator
{
    public interface IEntitySnapshotProvider
    {
        T GetSnapshot<T>();
        object GetSnapshot(Type t);
    }
}
