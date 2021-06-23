using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
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
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IHomeAssistantTokenValidationService _homeAssistantTokenValidationService;


        public AuthenticationMiddleware(
            RequestDelegate next,
            IHomeAssistantTokenValidationService homeAssistantTokenValidationService,
            ILogger<AuthenticationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _homeAssistantTokenValidationService = homeAssistantTokenValidationService;
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

            string? token = null;

            var authenticationHeader = context.Request.Headers[HeaderNames.Authorization];
            
            // SignalR sends this header as lowercase.
            if(authenticationHeader == StringValues.Empty)
                authenticationHeader = context.Request.Headers[HeaderNames.Authorization.ToLower()];

            if (authenticationHeader != StringValues.Empty)
            {
                if (!AuthenticationHeaderValue.TryParse(authenticationHeader, out var authenticationHeaderValue) ||
                    authenticationHeaderValue.Parameter is null)
                {
                    _logger.LogWarning($"Unauthorized request! Unable to parse authorization header. Request: {context.Request.GetDisplayUrl()}");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }

                token = authenticationHeaderValue.Parameter;
            }
            else if (context.Request.Query.ContainsKey("access_token"))
            {
                // SignalR sends the token as part of the query when upgrading the connection to websockets.
                token = context.Request.Query["access_token"];
            }

            if (token is null)
            {
                _logger.LogWarning($"Unauthorized request! Authorization header not set. Request: {context.Request.GetDisplayUrl()}");
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!await _homeAssistantTokenValidationService.IsValidAsync(token))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            await _next.Invoke(context);

            
        }
    }
}