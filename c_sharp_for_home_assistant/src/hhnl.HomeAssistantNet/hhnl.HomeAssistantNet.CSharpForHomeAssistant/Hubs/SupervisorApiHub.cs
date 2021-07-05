using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs
{
    public class SupervisorApiHub : Hub<ISupervisorApiClient>
    {
        private readonly IManagementHubCallService _managementHubCallService;

        public SupervisorApiHub(IManagementHubCallService managementHubCallService)
        {
            _managementHubCallService = managementHubCallService;
        }

        public override async Task OnConnectedAsync()
        {
            if (_managementHubCallService.DefaultConnection is not null)
                await Clients.Caller.OnConnectionChanged(_managementHubCallService.DefaultConnection);
        }
    }
}