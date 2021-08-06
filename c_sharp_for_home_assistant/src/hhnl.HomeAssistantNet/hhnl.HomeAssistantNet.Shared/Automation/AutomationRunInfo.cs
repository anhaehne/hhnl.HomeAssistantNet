using hhnl.HomeAssistantNet.Shared.Supervisor;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Automation
{
    public class AutomationRunInfo
    {
        private readonly static Dictionary<Type, object> _emptySnapshot = new();
        private readonly static Action _noop = () => { };

        public Guid Id { get; set; }

        public string? Error { get; set; }
        
        public DateTimeOffset Started { get; set; }
        
        public DateTimeOffset? Ended { get; set; }
        
        public RunState State { get; set; }

        public StartReason Reason { get; set; }

        public string? ReasonMessage { get; set; }

        [JsonIgnore]
        public List<LogMessageDto> Log { get; set; } = new List<LogMessageDto>();

        [JsonIgnore] 
        public Task Task { get; set; } = Task.CompletedTask;

        [JsonIgnore] 
        public Action Start { get; set; } = _noop;
        
        [JsonIgnore]
        public CancellationTokenSource? CancellationTokenSource { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<Type, object> EntitySnapshot { get; set; } = _emptySnapshot;

        [JsonIgnore]
        public int ServiceCallCount { get; set; }

        public enum RunState
        {
            Running,
            Completed,
            Cancelled,
            Error,
            WaitingInQueue,
        }
        
        public enum StartReason
        {
            RunOnStart,
            EntityChanged,
            Manual,
            Schedule,
            EventFired,
            Custom,
            RunOnConnect,
        }
    }
}