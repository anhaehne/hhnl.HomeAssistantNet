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
        Task<AutomationInfoDto?> StopAutomationAsync(string name, TimeSpan timeout);
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

        public async Task<IReadOnlyCollection<AutomationInfoDto>> GetAutomationsAsync()
        {
            _logger.LogDebug("Getting automation list.");
            var result = await _hubCallService.CallService<IReadOnlyCollection<AutomationInfoDto>>((id, client) =>
                client.GetAutomationsAsync(id));
            return result ?? Array.Empty<AutomationInfoDto>();
        }

        public Task<AutomationInfoDto?> StartAutomationAsync(string name)
        {
            _logger.LogDebug($"Starting automation '{name}'.");
            return _hubCallService.CallService<AutomationInfoDto>((id, client) => client.StartAutomationAsync(id, name));
        }

        public Task<AutomationInfoDto?> StopAutomationAsync(string name, TimeSpan timeout)
        {
            _logger.LogDebug($"Stopping automation '{name}'.");
            return _hubCallService.CallService<AutomationInfoDto>((id, client) => client.StopAutomationAsync(id, name),
                timeout);
        }
    }
}