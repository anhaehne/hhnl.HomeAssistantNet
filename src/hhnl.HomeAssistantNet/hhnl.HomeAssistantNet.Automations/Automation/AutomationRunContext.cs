using System;
using System.Threading;
using hhnl.HomeAssistantNet.Shared.Automation;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class AutomationRunContext
    {
        private static readonly AsyncLocal<AutomationRunContext?> _current = new();

        public AutomationRunContext(CancellationToken token, IServiceProvider serviceProvider, AutomationRunInfo currentRun)
        {
            CancellationToken = token;
            ServiceProvider = serviceProvider;
            CurrentRun = currentRun;
        }

        public static AutomationRunContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        public CancellationToken CancellationToken { get; }

        public IServiceProvider ServiceProvider { get; }

        public AutomationRunInfo CurrentRun { get; }

        public static AutomationRunContext GetRunContextOrFail()
        {
            return Current ?? throw new RunContextNotSetException();
        }

        public class RunContextNotSetException : Exception
        {
            public RunContextNotSetException()
                : base("The run context hasn't been set.")
            {
            }
        }
    }
}