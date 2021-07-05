using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
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
        private readonly IAutomationService _automationService;
        private readonly HubConnection? _hubConnection;
        private readonly ILogger<SupervisorClient> _logger;
        private readonly TaskCompletionSource _clientStarted = new ();

        public SupervisorClient(
            IAutomationRegistry automationRegistry,
            IAutomationService automationService,
            ILogger<SupervisorClient> logger,
            IOptions<AutomationsConfig> config,
            IOptions<HomeAssistantConfig> haConfig)
        {
            _automationRegistry = automationRegistry;
            _automationService = automationService;
            _logger = logger;

            if (config.Value.SupervisorUrl is null)
            {
                _logger.LogInformation("Supervisor not configured.");
                return;
            }

            _logger.LogInformation(
                $"Setup supervisor client Url '{config.Value.SupervisorUrl}' Token '{haConfig.Value.SUPERVISOR_TOKEN.Substring(0, 10)}...'");

            var connectUri = new Uri(new Uri(config.Value.SupervisorUrl), "/api/client-management");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(connectUri,
                    options => options.AccessTokenProvider = () => Task.FromResult(haConfig.Value.SUPERVISOR_TOKEN))
                .WithAutomaticReconnect()
                .Build();
            _hubConnection.On<long>(nameof(IManagementClient.GetAutomationsAsync), GetAutomationsAsync);
            _hubConnection.On<long, string>(nameof(IManagementClient.StartAutomationAsync), StartAutomationAsync);
            _hubConnection.On<long, Guid>(nameof(IManagementClient.StopAutomationRunAsync), StopAutomationRunAsync);
            _hubConnection.On(nameof(IManagementClient.Shutdown), Shutdown);
            _hubConnection.On<long>(nameof(IManagementClient.GetProcessId), GetProcessId);
            _hubConnection.On<long, Guid>(nameof(IManagementClient.StartListenToRunLog), StartListenToRunLog);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection is null)
                return;

            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Supervisor client started");
            _clientStarted.SetResult();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection is null)
                return Task.CompletedTask;

            _logger.LogInformation("Stopping supervisor client ...");
            return _hubConnection.StopAsync(cancellationToken);
        }

        public async Task StartAutomationAsync(long messageId, string name)
        {
            if (!_automationRegistry.Automations.TryGetValue(name, out var automation))
            {
                await _hubConnection.SendAsync("AutomationStarted", messageId, null);
                return;
            }

            await _automationService.EnqueueAutomationForManualStartAsync(automation);
            await _hubConnection.SendAsync("AutomationStarted", messageId, ToDto(automation));
        }

        public async Task StopAutomationRunAsync(long messageId, Guid runId)
        {
            var entry = _automationRegistry.Automations.SelectMany(x => x.Value.Runs.Select(run => (Automation: x.Value, Run: run))).SingleOrDefault(x => x.Run.Id == runId);

            if (entry == default)
            {
                await _hubConnection.SendAsync("AutomationRunStopped", messageId);
                return;
            }

            await _automationService.StopAutomationRunAsync(entry.Automation, entry.Run);
            await _hubConnection.SendAsync("AutomationRunStopped", messageId);
        }

        public Task GetAutomationsAsync(long messageId)
        {
            var automations = _automationRegistry.Automations.Values.Select(ToDto).ToArray();
            return _hubConnection.SendAsync("AutomationsGot", messageId, automations);
        }

        public async Task StartListenToRunLog(long messageId, Guid runId)
        {
            var result = _automationRegistry.Automations.SelectMany(x => x.Value.Runs).SingleOrDefault(r => r.Id == runId)?.Log;
            await _hubConnection.SendAsync("StartedListingToRunLog", messageId, result);
            AutomationLogger.RegisterRun(runId);
        }

        public Task StopListenToRunLog(long messageId, Guid runId)
        {
            AutomationLogger.UnregisterRun(runId);
            return _hubConnection.SendAsync("StoppedListingToRunLog", messageId, true);
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

        public async Task OnAutomationsChanged()
        {
            var automations = _automationRegistry.Automations.Values.Select(ToDto).ToArray();

            // This can try to send messages before the client is connected so we have to wait first.
            await _clientStarted.Task;
            await _hubConnection.SendAsync("OnAutomationsChanged", automations);
        }

        public async Task OnNewLogMessage(LogMessageDto logMessageDto)
        {
            // This can try to send messages before the client is connected so we have to wait first.
            await _clientStarted.Task;
            await _hubConnection.SendAsync("OnNewLogMessage", logMessageDto);
        }

        private static AutomationInfoDto ToDto(AutomationEntry entry)
        {
            return new AutomationInfoDto(entry.Info, entry.Runs);
        }
    }
}