using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.Extensions.Logging;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IAutomationsHostService
    {
        Task<IReadOnlyCollection<AutomationInfoDto>> GetAutomationsAsync();
        Task<AutomationInfoDto?> StartAutomationAsync(string name);
        Task StopAutomationRunAsync(Guid runId);
    }

    public class AutomationsService : IAutomationsHostService
    {
        private readonly IManagementHubCallService _managementHubCallService;
        private readonly ILogger<AutomationsService> _logger;

        public AutomationsService(IManagementHubCallService managementHubCallService, ILogger<AutomationsService> logger)
        {
            _managementHubCallService = managementHubCallService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<AutomationInfoDto>> GetAutomationsAsync()
        {
            _logger.LogDebug("Getting automation list.");
            var result = await _managementHubCallService.CallService<IReadOnlyCollection<AutomationInfoDto>>((id, client) =>
                client.GetAutomationsAsync(id));
            return result ?? Array.Empty<AutomationInfoDto>();
        }

        public Task<AutomationInfoDto?> StartAutomationAsync(string name)
        {
            _logger.LogDebug($"Starting automation '{name}'.");
            return _managementHubCallService.CallService<AutomationInfoDto>((id, client) => client.StartAutomationAsync(id, name));
        }

        public Task StopAutomationRunAsync(Guid runId)
        {
            _logger.LogDebug($"Stopping automation run '{runId}'.");
            return _managementHubCallService.CallService<bool>((id, client) => client.StopAutomationRunAsync(id, runId));
        }
    }
}