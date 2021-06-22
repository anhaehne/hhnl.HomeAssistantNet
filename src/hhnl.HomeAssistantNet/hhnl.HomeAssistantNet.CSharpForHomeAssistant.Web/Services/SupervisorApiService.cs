using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Supervisor;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services
{
    public class SupervisorApiService
    {
        private readonly AuthenticationService _authenticationService;
        private readonly HttpClient _httpClient;

        public SupervisorApiService(HttpClient httpClient, AuthenticationService authenticationService)
        {
            _httpClient = httpClient;
            _authenticationService = authenticationService;
        }

        public ApplicationState State { get; private set; } = ApplicationState.ConnectedToHost;
        
        public async Task<IReadOnlyCollection<AutomationInfoDto>> GetAutomationsAsync()
        {
            return await GetAsync<IReadOnlyCollection<AutomationInfoDto>>("api/automation") ??
                   ArraySegment<AutomationInfoDto>.Empty;
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
            
            // TODO renew automations
            await GetAutomationsAsync();
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
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.FailedDependency)
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
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.FailedDependency)
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
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _authenticationService.Token);
            
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

        public enum ApplicationState
        {
            Connecting,
            ConnectedToHost,
            BuildAndDeploy,
            NoConnection
        } 
    }
}