using System;

namespace hhnl.HomeAssistantNet.Automation
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AutomationAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayName">
        /// The display name of the automation.</param>
        /// <param name="runOnStart">
        /// When set to <c>true</c> the automation will be executed when the application is started.
        /// </param>
        public AutomationAttribute(string? displayName = null, bool runOnStart = false)
        {
        }
    }
}