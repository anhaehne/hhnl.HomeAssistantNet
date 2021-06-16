using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.Supervisor
{
    public interface IManagementClient
    {
        Task StartAutomationAsync(long messageId, string name);
        
        Task StopAutomationAsync(long messageId, string name);

        Task GetAutomationsAsync(long messageId);

        Task Shutdown();

        Task GetProcessId(long messageId);
    }
}