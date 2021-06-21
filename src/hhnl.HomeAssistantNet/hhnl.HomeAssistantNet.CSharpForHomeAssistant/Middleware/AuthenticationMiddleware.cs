using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<HomeAssistantConfig> _haConfig;


        public AuthenticationMiddleware(RequestDelegate next, IMemoryCache  memoryCache, IHttpClientFactory httpClientFactory, IOptions<HomeAssistantConfig> haConfig)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _memoryCache = memoryCache;
            _httpClientFactory = httpClientFactory;
            _haConfig = haConfig;
        }
        
        public async Task Invoke(HttpContext context)
        {
            if (HomeAssistantIngress.RequestIsViaIngress(context))
            {
                await _next.Invoke(context);
                return;
            }

            var authenticationHeader = context.Request.Headers[HeaderNames.Authorization];

            if (authenticationHeader == StringValues.Empty)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!AuthenticationHeaderValue.TryParse(authenticationHeader, out var authenticationHeaderValue) || authenticationHeaderValue.Parameter is null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }
            
            var cachedToken = _memoryCache.Get<JsonWebToken>(authenticationHeaderValue.Parameter);
            if (cachedToken is not null && cachedToken.ValidTo >= DateTime.Now)
            {
                // We know this token already and it is valid.
                await _next.Invoke(context);
                return;
            }
            
            var jwt = new JsonWebToken(authenticationHeaderValue.Parameter);
            if (jwt.ValidTo < DateTime.Now)
            {
                // Token expired.
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            var isValid = await CheckTokenAsync(authenticationHeaderValue);

            if (!isValid)
            {
                // Token expired.
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            _memoryCache.GetOrCreate(authenticationHeaderValue.Parameter,
                entry =>
                {
                    entry.AbsoluteExpiration = jwt.ValidTo;
                    return jwt;
                });
            
            await _next.Invoke(context);
        }

        private async Task<bool> CheckTokenAsync(AuthenticationHeaderValue headerValue)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(new Uri(_haConfig.Value.Instance), "/api/"),
                Headers =
                {
                    { HeaderNames.Authorization, headerValue.ToString() }
                }
            };

            var response = await httpClient.SendAsync(request);

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => false,
                HttpStatusCode.OK => true,
                _ => throw new InvalidOperationException($"Got unexpected status code {response.StatusCode}"),
            };
        }
    }
}