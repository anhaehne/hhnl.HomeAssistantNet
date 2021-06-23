using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
        private readonly BehaviorSubject<ConnectionInfo?> _connectionInfoSubject = new(null);
        private HubConnection? _hubConnection;

        public SupervisorApiService(HttpClient httpClient, AuthenticationService authenticationService, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
            _navigationManager = navigationManager;
        }

        public ApplicationState State { get; private set; } = ApplicationState.ConnectedToHost;

        public IObservable<ConnectionInfo?> Connection => _connectionInfoSubject;

        public async Task StartAsync()
        {
            var builder = new HubConnectionBuilder()
                .WithAutomaticReconnect();

            var needToken = await _authenticationService.NeedsTokenAsync();

            if (needToken)
            {
                builder.WithUrl(_navigationManager.ToAbsoluteUri("api/supervisor-api"),
                    options => options.AccessTokenProvider = async () => await _authenticationService.GetTokenAsync());
            }
            else
                builder.WithUrl(_navigationManager.ToAbsoluteUri("api/supervisor-api"));

            _hubConnection = builder.Build();

            _hubConnection.On<ConnectionInfo?>(nameof(ISupervisorApiClient.OnConnectionChanged),
                info => _connectionInfoSubject.OnNext(info));

            await _hubConnection.StartAsync();
        }

        public Task StartAutomationAsync(AutomationInfoDto automation)
        {
            return PostAsync($"api/automation/{automation.Info.Name}/start");
        }

        public async Task BuildAndDeployAsync()
        {
            State = ApplicationState.BuildAndDeploy;
            await PostAsync("api/build/deploy");
            State = ApplicationState.Connecting;
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