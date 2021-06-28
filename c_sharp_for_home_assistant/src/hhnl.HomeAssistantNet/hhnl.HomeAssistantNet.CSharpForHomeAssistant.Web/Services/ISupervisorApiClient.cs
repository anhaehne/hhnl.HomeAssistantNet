using hhnl.HomeAssistantNet.Shared.Supervisor;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services
{
    public interface ISupervisorApiClient
    {
        Task OnConnectionChanged(ConnectionInfo? connection);

        Task OnNewLogMessage(LogMessageDto logMessageDto);
    }
}