using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blazored.Modal;
using Blazored.Modal.Services;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Components;
using Microsoft.Net.Http.Headers;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IModalService _modalService;
        private bool? _needToken; 

        public AuthenticationService(HttpClient httpClient, IModalService modalService)
        {
            _httpClient = httpClient;
            _modalService = modalService;
        }
        
        public string? Token { get; private set; }

        public bool TokenRequestInProgress { get; private set; }

        private TaskCompletionSource? _tokenRequestTcs;
        private readonly object _tokenRequestTcsLock = new();

        public async Task<string> GetTokenAsync()
        {
            if (Token is null)
                await WaitForTokenRequestAsync();
                
            return Token!;
        }

        public Task WaitForTokenRequestAsync()
        {
            TokenRequestInProgress = true;
            _modalService.Show<Login>("Login", new ModalOptions
            {
                HideCloseButton = true,
            });

            lock (_tokenRequestTcsLock)
            {
                _tokenRequestTcs ??= new TaskCompletionSource();
                return _tokenRequestTcs.Task;
            }
        }

        public async Task<bool> NeedsTokenAsync()
        {
            if (_needToken.HasValue)
                return _needToken.Value;
            
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("api", UriKind.Relative),
            };

            var response = await _httpClient.SendAsync(request);
            
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    _needToken = false;
                    return false;
                case HttpStatusCode.Unauthorized:
                    _needToken = true;
                    return true;
                default:
                    throw new HttpRequestException($"Unexpected status code {response.StatusCode} from supervisor api.");
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("api", UriKind.Relative),
                Headers =
                {
                    { HeaderNames.Authorization, $"Bearer {token}" },
                }
            };

            var response = await _httpClient.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Token = token;
                    _tokenRequestTcs?.TrySetResult();
                    _tokenRequestTcs = null;
                    TokenRequestInProgress = false;
                    return true;
                case HttpStatusCode.Unauthorized:
                    return false;
                default:
                    throw new HttpRequestException($"Unexpected status code {response.StatusCode} from supervisor api.");
            }
        }
    }
}