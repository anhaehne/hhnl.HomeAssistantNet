using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly IOptions<HomeAssistantConfig> _haConfig;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestDelegate _next;


        public AuthenticationMiddleware(
            RequestDelegate next,
            IMemoryCache memoryCache,
            IHttpClientFactory httpClientFactory,
            IOptions<HomeAssistantConfig> haConfig,
            ILogger<AuthenticationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _memoryCache = memoryCache;
            _httpClientFactory = httpClientFactory;
            _haConfig = haConfig;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // TODO: implement proper asp.net core authentication
            if (HomeAssistantIngress.RequestIsViaIngress(context))
            {
                await _next.Invoke(context);
                return;
            }

            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next.Invoke(context);
                return;
            }

            var authenticationHeader = context.Request.Headers[HeaderNames.Authorization];

            if (authenticationHeader == StringValues.Empty)
            {
                _logger.LogWarning($"Unauthorized request! Authorization header not set. Request: {context.Request.GetDisplayUrl()}");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!AuthenticationHeaderValue.TryParse(authenticationHeader, out var authenticationHeaderValue) ||
                authenticationHeaderValue.Parameter is null)
            {
                _logger.LogWarning($"Unauthorized request! Unable to parse authorization header. Request: {context.Request.GetDisplayUrl()}");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            var cachedToken = _memoryCache.Get<object>(authenticationHeaderValue.Parameter);
            if (cachedToken is not null && (cachedToken is not JsonWebToken cachedJwt || cachedJwt.ValidTo > DateTime.Now))
            {
                // We know this token already and it is valid.
                await _next.Invoke(context);
                return;
            }

            // Long lived access tokens are JWTs. The supervisor token is only generic string.
            if (TryGetJwt(authenticationHeaderValue.Parameter, out var jwt))
            {
                if (jwt.ValidTo < DateTime.Now)
                {
                    _logger.LogWarning($"Unauthorized request! Token expired '{jwt.ValidTo}'. Request: {context.Request.GetDisplayUrl()}");
                    // Token expired.
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }
            }

            var isValid = await CheckTokenAsync(authenticationHeaderValue);

            if (!isValid)
            {
                _logger.LogWarning($"Unauthorized request! Token validation failed. Home assistant responded with 401. Request: {context.Request.GetDisplayUrl()}");
                // Token expired.
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            _memoryCache.GetOrCreate<object>(authenticationHeaderValue.Parameter,
                entry =>
                {
                    if(jwt is not null)
                        entry.AbsoluteExpiration = jwt.ValidTo;
                    
                    return (object?)jwt ?? authenticationHeaderValue.Parameter;
                });

            await _next.Invoke(context);

            static bool TryGetJwt(string value, [NotNullWhen(true)] out JsonWebToken? token)
            {
                var handler = new JsonWebTokenHandler();

                if (!handler.CanReadToken(value))
                {
                    token = null;
                    return false;
                }

                token = handler.ReadJsonWebToken(value);
                return true;
            }
        }

        private async Task<bool> CheckTokenAsync(AuthenticationHeaderValue headerValue)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(new Uri(_haConfig.Value.Instance), "api/"),
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
                _ => throw new InvalidOperationException($"Got unexpected status code {response.StatusCode}")
            };
        }
    }
}