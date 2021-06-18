using System;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    /// <summary>
    /// Marks a parameter as not being tracked by the change detection.
    /// This will lead to the automation not being executed when the Entity parameter,
    /// marked with this attribute, has changed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class NoTrackAttribute : Attribute
    {
    }
}