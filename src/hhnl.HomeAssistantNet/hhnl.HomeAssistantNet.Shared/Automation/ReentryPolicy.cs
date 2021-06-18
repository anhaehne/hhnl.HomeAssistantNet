using System.Threading;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    /// <summary>
    /// The policy to use when an automation is triggered while the previous execution hasn't finished yet.
    /// All policies that queue execution will only ever queue one execution. If an execution is already queued, the next one
    /// will be discarded.
    /// </summary>
    public enum ReentryPolicy
    {
        /// <summary>
        /// Will queue the execution and wait for the previous execution to complete.
        /// </summary>
        Queue,

        /// <summary>
        /// Will discard the new execution.
        /// </summary>
        Discard,

        /// <summary>
        /// Will queue the execution and tries to cancel the previous one.
        /// </summary>
        CancelPrevious,

        /// <summary>
        /// Will run the automation without checking for previous execution.
        /// This can cause unexpected behaviour.
        /// </summary>
        Allow
    }
}