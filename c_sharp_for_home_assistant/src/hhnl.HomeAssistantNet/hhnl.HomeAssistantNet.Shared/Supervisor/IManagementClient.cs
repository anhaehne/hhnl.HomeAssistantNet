using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public interface IManagementClient
    {
        Task StartAutomationAsync(long messageId, string name);
        
        Task StopAutomationRunAsync(long messageId, Guid runId);

        Task StartListenToRunLog(long messageId, Guid runId);

        Task StopListenToRunLog(long messageId, Guid runId);

        Task GetAutomationsAsync(long messageId);

        Task Shutdown();

        Task GetProcessId(long messageId);
    }
}