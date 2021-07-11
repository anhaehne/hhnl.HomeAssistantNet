using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Supervisor
{
    public class SupervisorClient : IHostedService, IManagementClient
    {
        private readonly IAutomationRegistry _automationRegistry;
        private readonly IAutomationService _automationService;
        private readonly HubConnection? _hubConnection;
        private readonly ILogger<SupervisorClient> _logger;
        private readonly Channel<(string MethodName, object? arg1)> _pushMessageChannel = Channel.CreateUnbounded<(string MethodName, object? arg1)>();
        private Task? _runTask;
        private CancellationTokenSource? _cts;

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

            _cts = new CancellationTokenSource();
            _runTask = RunAsync().ContinueWith(task =>
            {
                if (!_cts.IsCancellationRequested)
                    _logger.LogError("The run task has completed even though the runner hasn't been stopped.");
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();

            if (_runTask is not null)
                await _runTask;

            if (_hubConnection is not null)
            {
                _logger.LogInformation("Stopping supervisor client ...");
                await _hubConnection.StopAsync(cancellationToken);
            }
        }

        public async Task StartAutomationAsync(long messageId, string name)
        {
            if (!_automationRegistry.Automations.TryGetValue(name, out var automation))
            {
                await _hubConnection.SendAsync("AutomationStarted", messageId, null);
                return;
            }

            await _automationService.EnqueueAutomationAsync(automation, Shared.Automation.AutomationRunInfo.StartReason.Manual, waitForStart: true);
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
            await _pushMessageChannel.Writer.WriteAsync(("OnAutomationsChanged", automations));
        }

        public async Task OnNewLogMessage(LogMessageDto logMessageDto)
        {
            await _pushMessageChannel.Writer.WriteAsync(("OnNewLogMessage", logMessageDto));
        }

        private async Task RunAsync()
        {
            if (_cts is null)
                throw new InvalidOperationException("Cancellation token source is null.");

            while (!_cts.IsCancellationRequested)
            {
                var next = await _pushMessageChannel.Reader.ReadAsync(_cts.Token);

                try
                {
                    await _hubConnection.SendAsync(next.MethodName, next.arg1, cancellationToken: _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to invoke method {next.MethodName}");
                }
            }
        }

        private static AutomationInfoDto ToDto(AutomationEntry entry)
        {
            return new AutomationInfoDto(entry.Info, entry.Runs);
        }
    }
}