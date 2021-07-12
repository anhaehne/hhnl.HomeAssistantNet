using System;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    /// <summary>
    /// When applied to a parameter, the entity will always be a snapshot created at the start of the automation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class SnapshotAttribute : Attribute
    {
    }
}
