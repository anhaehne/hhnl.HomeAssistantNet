using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services
{
    public class SupervisorApiService
    {
        public enum ApplicationState
        {
            Connecting,
            ConnectedToHost,
            BuildAndDeploy,
            NoConnection
        }

        private readonly AuthenticationService _authenticationService;
        private readonly NavigationManager _navigationManager;
        private readonly HttpClient _httpClient;
        private readonly BehaviorSubject<SupervisorConnectionInfo?> _connectionInfoSubject = new(null);
        private readonly ConcurrentDictionary<Guid, ReplaySubject<LogMessageDto>> _logMessageSubjects = new();
        private HubConnection? _hubConnection;

        public SupervisorApiService(HttpClient httpClient, AuthenticationService authenticationService, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
            _navigationManager = navigationManager;
        }

        public ApplicationState State { get; private set; } = ApplicationState.ConnectedToHost;

        public IObservable<SupervisorConnectionInfo?> Connection => _connectionInfoSubject.Throttle(TimeSpan.FromMilliseconds(50));

        public async Task<IObservable<LogMessageDto>> ListenToRunLogMessagesAsync(Guid runId)
        {
            var subject = _logMessageSubjects.GetOrAdd(runId, runId => new ReplaySubject<LogMessageDto>());

            var messages = await PostAsync<IReadOnlyCollection<LogMessageDto>>($"api/run/{runId}/logs/start-listen") ?? Array.Empty<LogMessageDto>();

            foreach (var message in messages)
            {
                subject.OnNext(message);
            }

            return subject;
        }

        public async Task<IObservable<LogMessageDto>> ListenToBuildLogMessagesAsync(Guid runId)
        {
            return _logMessageSubjects.GetOrAdd(runId, runId => new ReplaySubject<LogMessageDto>());
        }

        public async Task StopListenToLogMessagesAsync(Guid runId)
        {
            await PostAsync($"api/run/{runId}/logs/stop-listen");

            if (!_logMessageSubjects.TryRemove(runId, out var runSubject))
                return;

            runSubject.OnCompleted();
            runSubject.Dispose();
        }

        public async Task StartAsync()
        {
            var builder = new HubConnectionBuilder();
                //.WithAutomaticReconnect();

            var needToken = await _authenticationService.NeedsTokenAsync();

            if (needToken)
            {
                builder.WithUrl(_navigationManager.ToAbsoluteUri("api/supervisor-api"),
                    options => options.AccessTokenProvider = async () => await _authenticationService.GetTokenAsync());
            }
            else
                builder.WithUrl(_navigationManager.ToAbsoluteUri("api/supervisor-api"));

            _hubConnection = builder.Build();
            _hubConnection.Reconnecting += exception =>
            {
                _connectionInfoSubject.OnNext(null);
                return Task.CompletedTask;
            };

            _hubConnection.On<SupervisorConnectionInfo?>(nameof(ISupervisorApiClient.OnConnectionChanged),
                info => _connectionInfoSubject.OnNext(info));
            _hubConnection.On<LogMessageDto>(nameof(ISupervisorApiClient.OnNewLogMessage), message =>
            {
                if (_logMessageSubjects.TryGetValue(message.RunId, out var messageSubject))
                    messageSubject.OnNext(message);
            });

            await _hubConnection.StartAsync();
        }

        public Task StartAutomationAsync(AutomationInfoDto automation)
        {
            return PostAsync($"api/automation/{automation.Info.Name}/start");
        }
        
        public Task StopAutomationRunAsync(AutomationRunInfo runInfo)
        {
            return PostAsync($"api/run/{runInfo.Id}/stop");
        }

        public async Task<Guid> StartBuildAndDeployAsync()
        {
            var guid = await PostAsync<Guid>("api/build/start-deploy");

            if(guid != Guid.Empty)
                State = ApplicationState.BuildAndDeploy;

            return guid;
        }

        public async Task WaitForBuildAndDeployAsync()
        {
            await PostAsync("api/build/wait-for-deploy");
            State = ApplicationState.Connecting;
        }

        public async Task<AutomationSecrets> GetSecretsAsync()
        {
            return await GetAsync<AutomationSecrets>("api/secrets") ?? AutomationSecrets.Empty;
        }

        public Task SaveSecretsAsync(AutomationSecrets secrets)
        {
            return PostAsync("api/secrets", secrets);
        }

        private async Task<T?> GetAsync<T>(string uri)
        {
            while (true)
            {
                try
                {
                    var result = await SendWithAuthorizationRetry(client => client.GetFromJsonAsync<T>(uri));

                    if (State != ApplicationState.BuildAndDeploy)
                        State = ApplicationState.ConnectedToHost;

                    return result;
                }
                catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.FailedDependency or HttpStatusCode
                    .RequestTimeout)
                {
                    State = ApplicationState.NoConnection;
                }
            }
        }

        private async Task PostAsync(string uri)
        {
            try
            {
                var responseMessage = await SendWithAuthorizationRetry(client => client.PostAsJsonAsync<object>(uri, null!));
                responseMessage.EnsureSuccessStatusCode();

                if (State != ApplicationState.BuildAndDeploy)
                    State = ApplicationState.ConnectedToHost;
            }
            catch (HttpRequestException e) when (
                e.StatusCode is HttpStatusCode.FailedDependency or HttpStatusCode.RequestTimeout)
            {
                State = ApplicationState.NoConnection;
            }
        }
        private async Task<T?> PostAsync<T>(string uri)
        {
            try
            {
                var responseMessage = await SendWithAuthorizationRetry(client => client.PostAsJsonAsync<object>(uri, null!));
                responseMessage.EnsureSuccessStatusCode();

                if (State != ApplicationState.BuildAndDeploy)
                    State = ApplicationState.ConnectedToHost;

                return await responseMessage.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException e) when (
                e.StatusCode is HttpStatusCode.FailedDependency or HttpStatusCode.RequestTimeout)
            {
                State = ApplicationState.NoConnection;
                return default;
            }
        }

        private async Task PostAsync<T>(string uri, T body)
        {
            try
            {
                var responseMessage = await SendWithAuthorizationRetry(client => client.PostAsJsonAsync(uri, body));
                responseMessage.EnsureSuccessStatusCode();

                if (State != ApplicationState.BuildAndDeploy)
                    State = ApplicationState.ConnectedToHost;
            }
            catch (HttpRequestException e) when (
                e.StatusCode is HttpStatusCode.FailedDependency or HttpStatusCode.RequestTimeout)
            {
                State = ApplicationState.NoConnection;
            }
        }

        private async Task SendWithAuthorizationRetry(Func<HttpClient, Task> send)
        {
            await SendWithAuthorizationRetry(async client =>
            {
                await send(client);
                return Task.FromResult(true);
            });
        }

        private async Task<T> SendWithAuthorizationRetry<T>(Func<HttpClient, Task<T>> send)
        {
            if (!string.IsNullOrEmpty(_authenticationService.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authenticationService.Token);
            }

            try
            {
                return await send(_httpClient);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Request token and try again.
                await _authenticationService.WaitForTokenRequestAsync();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authenticationService.Token);
                return await send(_httpClient);
            }
        }
    }
}