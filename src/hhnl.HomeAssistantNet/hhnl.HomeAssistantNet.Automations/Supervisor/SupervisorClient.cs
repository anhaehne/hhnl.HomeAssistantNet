using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.Automations.Supervisor
{
    public class SupervisorClient : IHostedService, IManagementClient
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IAutomationRunner _automationRunner;
        private readonly HubConnection? _hubConnection;
        private readonly ILogger<SupervisorClient> _logger;

        public SupervisorClient(
            IAutomationRegistry automationRegistry,
            IAutomationRunner automationRunner,
            ILogger<SupervisorClient> logger,
            IOptions<AutomationsConfig> config)
        {
            _automationRegistry = automationRegistry;
            _automationRunner = automationRunner;
            _logger = logger;

            if (config.Value.SupervisorUrl is null)
            {
                _logger.LogInformation("Supervisor not configured.");
                return;
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri($"{config.Value.SupervisorUrl}/client-management"))
                .WithAutomaticReconnect()
                .Build();
            _hubConnection.On<long>(nameof(IManagementClient.GetAutomationsAsync), GetAutomationsAsync);
            _hubConnection.On<long, string>(nameof(IManagementClient.StartAutomationAsync), StartAutomationAsync);
            _hubConnection.On<long, string>(nameof(IManagementClient.StopAutomationAsync), StopAutomationAsync);
            _hubConnection.On(nameof(IManagementClient.Shutdown), Shutdown);
            _hubConnection.On<long>(nameof(IManagementClient.GetProcessId), GetProcessId);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection is null)
                return;

            _logger.LogInformation("Starting supervisor client ...");
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Supervisor client started");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection is null)
                return Task.CompletedTask;

            _logger.LogInformation("Stopping supervisor client ...");
            return _hubConnection.StopAsync(cancellationToken);
        }

        public Task StartAutomationAsync(long messageId, string name)
        {
            if (!_automationRegistry.Automations.TryGetValue(name, out var automation))
                return _hubConnection.SendAsync("AutomationStarted", messageId, null);

            _automationRunner.RunAutomation(automation);
            return _hubConnection.SendAsync("AutomationStarted", messageId, ToManagementInfo(automation));
        }

        public Task StopAutomationAsync(long messageId, string name)
        {
            throw new NotImplementedException();
        }

        public Task GetAutomationsAsync(long messageId)
        {
            var automations = _automationRegistry.Automations.Values.Select(ToManagementInfo).ToList();
            return _hubConnection.SendAsync("AutomationsGot", messageId, automations);
        }

        public Task Shutdown()
        {
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        public Task GetProcessId(long messageId)
        {
            return _hubConnection.SendAsync("ProcessIdGot", messageId, Process.GetCurrentProcess().Id);
        }

        private static ManagementAutomationInfo ToManagementInfo(AutomationRunInfo runInfo)
        {
            return new ManagementAutomationInfo
            {
                Name = runInfo.Info.Name,
                FriendlyName = runInfo.Info.DisplayName,
                Running = false,
                LastError = null
            };
        }
    }
}