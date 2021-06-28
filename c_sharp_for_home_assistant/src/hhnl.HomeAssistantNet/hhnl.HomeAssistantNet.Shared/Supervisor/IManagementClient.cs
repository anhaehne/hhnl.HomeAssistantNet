using System;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public interface IManagementClient
    {
        Task StartAutomationAsync(long messageId, string name);
        
        Task StopAutomationAsync(long messageId, string name);

        Task StartListenToRunLog(long messageId, Guid runId);

        Task StopListenToRunLog(long messageId, Guid runId);

        Task GetAutomationsAsync(long messageId);

        Task Shutdown();

        Task GetProcessId(long messageId);
    }
}