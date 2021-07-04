using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Shared.HomeAssistantConnection
{
    public interface IHomeAssistantClient
    {
        Task<JsonElement> FetchStatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls a service.
        /// </summary>
        /// <param name="domain">The domain of the service.</param>
        /// <param name="service">The name of the service</param>
        /// <param name="serviceData">The data passed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<JsonElement> CallServiceAsync(
            string domain,
            string service,
            dynamic? serviceData = null,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Calls a service.
        /// </summary>
        /// <param name="domain">The domain of the service.</param>
        /// <param name="service">The name of the service</param>
        /// <param name="targetId">The target entity id</param>
        /// <param name="serviceData">The data passed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<JsonElement> CallServiceAsync(string domain, string service, string targetId, dynamic? serviceData = null, CancellationToken cancellationToken = default);
    }
}