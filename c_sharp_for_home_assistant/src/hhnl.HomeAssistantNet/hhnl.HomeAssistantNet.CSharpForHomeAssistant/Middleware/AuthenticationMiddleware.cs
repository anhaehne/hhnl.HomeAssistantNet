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

            if (!await _homeAssistantTokenValidationService.IsValidAsync(authenticationHeaderValue.Parameter))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            await _next.Invoke(context);

            
        }
    }
}