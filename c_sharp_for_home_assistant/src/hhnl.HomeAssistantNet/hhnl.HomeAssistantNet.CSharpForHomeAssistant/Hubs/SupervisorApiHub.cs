using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs
{
    public class SupervisorApiHub : Hub<ISupervisorApiClient>
    {
        private readonly IAutomationsHostService _hostService;
        private readonly IManagementHubCallService _managementHubCallService;

        public SupervisorApiHub(IAutomationsHostService hostService, IManagementHubCallService managementHubCallService)
        {
            _hostService = hostService;
            _managementHubCallService = managementHubCallService;
        }

        public override async Task OnConnectedAsync()
        {
            if (_managementHubCallService.DefaultConnection is not null)
                await Clients.Caller.OnConnectionChanged(_managementHubCallService.DefaultConnection);
        }

        private async Task<(bool Success, T? Result)> TryCallHostService<T>(Func<IAutomationsHostService, Task<T?>> serviceCalls)
        {
            T? result;

            try
            {
                result = await serviceCalls(_hostService);
            }
            catch (TaskCanceledException)
            {
                return (false, default);
            }
            catch (NoAutomationHostConnectedException)
            {
                return (false, default);
            }

            return (true, result);
        }
    }
}