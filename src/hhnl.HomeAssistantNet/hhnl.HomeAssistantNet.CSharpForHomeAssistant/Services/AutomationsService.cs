using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IAutomationsHostService
    {
        Task<IReadOnlyCollection<ManagementAutomationInfo>> GetAutomationsAsync();
        Task<ManagementAutomationInfo?> StartAutomationAsync(string name);
        Task<ManagementAutomationInfo?> StopAutomationAsync(string name, TimeSpan timeout);
    }

    public class AutomationsService : IAutomationsHostService
    {
        private readonly IHubCallService _hubCallService;
        private readonly ILogger<AutomationsService> _logger;

        public AutomationsService(IHubCallService hubCallService, ILogger<AutomationsService> logger)
        {
            _hubCallService = hubCallService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<ManagementAutomationInfo>> GetAutomationsAsync()
        {
            _logger.LogDebug("Getting automation list.");
            var result = await _hubCallService.CallService<IReadOnlyCollection<ManagementAutomationInfo>>((id, client) =>
                client.GetAutomationsAsync(id));
            return result ?? Array.Empty<ManagementAutomationInfo>();
        }

        public Task<ManagementAutomationInfo?> StartAutomationAsync(string name)
        {
            _logger.LogDebug($"Starting automation '{name}'.");
            return _hubCallService.CallService<ManagementAutomationInfo>((id, client) => client.StartAutomationAsync(id, name));
        }

        public Task<ManagementAutomationInfo?> StopAutomationAsync(string name, TimeSpan timeout)
        {
            _logger.LogDebug($"Stopping automation '{name}'.");
            return _hubCallService.CallService<ManagementAutomationInfo>((id, client) => client.StopAutomationAsync(id, name),
                timeout);
        }
    }
}