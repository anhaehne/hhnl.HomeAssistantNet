using System;
using JetBrains.Annotations;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class AutomationAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayName">
        /// The display name of the automation.</param>
        /// <param name="reentryPolicy">
        /// The policy to use when an automation is triggered while the previous execution hasn't finished yet.
        /// See: <see cref="ReentryPolicy"/>
        /// </param>
        public AutomationAttribute(string? displayName = null, ReentryPolicy reentryPolicy = ReentryPolicy.QueueLatest)
        {
            DisplayName = displayName;
            ReentryPolicy = reentryPolicy;
        }

        public string? DisplayName { get; }

        public ReentryPolicy ReentryPolicy { get; }
    }
}