using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Supervisor;
using hhnl.HomeAssistantNet.Shared.Automation;
using MediatR;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public class AutomationRunStateChangedNotification : INotification
    {
        public AutomationRunStateChangedNotification(AutomationEntry entry, AutomationRunInfo run)
        {
            Entry = entry;
            Run = run;
        }

        public AutomationEntry Entry { get; }

        public AutomationRunInfo Run { get; }

        public class Handler : INotificationHandler<AutomationRunStateChangedNotification>
        {
            private readonly SupervisorClient _supervisorClient;

            public Handler(SupervisorClient supervisorClient)
            {
                _supervisorClient = supervisorClient;
            }

            public Task Handle(AutomationRunStateChangedNotification notification, CancellationToken cancellationToken)
            {
                return _supervisorClient.OnAutomationsChanged();
            }
        }
    }
}