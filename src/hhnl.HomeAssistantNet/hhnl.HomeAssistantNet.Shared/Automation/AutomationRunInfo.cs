using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    public class AutomationRunInfo
    {
        public string? Error { get; set; }
        
        public DateTimeOffset Started { get; set; }
        
        public DateTimeOffset? Ended { get; set; }
        
        public RunState State { get; set; }

        [JsonIgnore]
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        
        public enum RunState
        {
            Running,
            Completed,
            Cancelled,
            Error,
        }
    }
}