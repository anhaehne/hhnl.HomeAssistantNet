using System.Collections.Generic;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Supervisor;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services
{
    public interface ISupervisorApiClient
    {
        Task OnConnectionChanged(ConnectionInfo? connection);
    }

    
}