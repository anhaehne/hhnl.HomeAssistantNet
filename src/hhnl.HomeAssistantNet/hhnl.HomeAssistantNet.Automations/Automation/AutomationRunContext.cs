using System;
using System.Threading;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class AutomationRunContext
    {
        private static readonly AsyncLocal<AutomationRunContext?> _current = new();

        public AutomationRunContext(CancellationToken token, IServiceProvider serviceProvider)
        {
            CancellationToken = token;
            ServiceProvider = serviceProvider;
        }

        public static AutomationRunContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        public CancellationToken CancellationToken { get; }

        public IServiceProvider ServiceProvider { get; }

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