using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IHomeAssistantTokenValidationService
    {
        Task<bool> IsValidAsync(string token);
    }

    public class HomeAssistantTokenValidationService : IHomeAssistantTokenValidationService
    {
        private readonly IOptions<HomeAssistantConfig> _haConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeAssistantTokenValidationService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly JsonWebTokenHandler _tokenHandler;

        public HomeAssistantTokenValidationService(
            IHttpClientFactory httpClientFactory,
            IOptions<HomeAssistantConfig> haConfig,
            IMemoryCache memoryCache,
            ILogger<HomeAssistantTokenValidationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _haConfig = haConfig;
            _memoryCache = memoryCache;
            _logger = logger;
            _tokenHandler = new JsonWebTokenHandler();

            if (!IsHostedInAddOn())
            {
                _logger.LogWarning("Not running in an add-on. This should only appear during development.");
                
                if(!File.Exists("/config/.storage/auth"))
                    _logger.LogWarning("No access to auth config.");
                
                if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN")))
                    _logger.LogWarning("SUPERVISOR_TOKEN not set.");
            }
        }

        public async Task<bool> IsValidAsync(string token)
        {
            // If the token is the current supervisor token, we know it is valid.
            if (IsHostedInAddOn() && Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN") == token)
                return true;

            var cachedToken = _memoryCache.Get<JsonWebToken>(token);

            if (cachedToken is not null && cachedToken.ValidTo > DateTime.Now)
                return true;

            if (!TryGetJwt(token, out var jwt))
            {
                _logger.LogWarning("Unauthorized request! Invalid JWT.");
                return false;
            }

            if (IsHostedInAddOn())
            {
                // When we are running inside an add-on we can access the auth config to validate the token.
                if (!await ValidateJwt(jwt))
                {
                    _logger.LogWarning("Unauthorized request! Token validation failed. Token not found or invalid.");
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Trying to validate JWT by request. This should only happen during development.");

                // We are currently not running inside an add-on and have no access to the home assistant config folder.
                // The only way to validate the token is by sending a request with the token to ha.
                if (!await ValidateTokenViaRequestAsync(token))
                {
                    _logger.LogWarning(
                        "Unauthorized request! Token validation failed. Home assistant responded with 401 Unauthorized.");
                    return false;
                }
            }

            _memoryCache.GetOrCreate<object>(token,
                entry =>
                {
                    entry.AbsoluteExpiration = jwt.ValidTo;
                    return jwt;
                });

            return true;
        }

        private static bool IsHostedInAddOn()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN")) &&
                   File.Exists("/config/.storage/auth");
        }

        private bool TryGetJwt(string value, [NotNullWhen(true)] out JsonWebToken? token)
        {
            if (!_tokenHandler.CanReadToken(value))
            {
                token = null;
                return false;
            }

            token = _tokenHandler.ReadJsonWebToken(value);
            return true;
        }

        private async Task<bool> ValidateJwt(JsonWebToken token)
        {
            await using var fs = File.OpenRead("/config/.storage/auth");
            var config = await JsonSerializer.DeserializeAsync<AuthConfig>(fs);

            if (config is null)
            {
                _logger.LogError("Unable to load auth config.");
                return false;
            }

            var tokenEntry = config?.Data.RefreshTokens.SingleOrDefault(t => t.Id == token.Issuer);

            if (tokenEntry is null)
            {
                // Token not found.
                return false;
            }

            var result = _tokenHandler.ValidateToken(token.EncodedToken,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenEntry.Key))
                });

            return result.IsValid;
        }

        private async Task<bool> ValidateTokenViaRequestAsync(string token)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(new Uri(_haConfig.Value.HOME_ASSISTANT_API), "api/"),
                Headers =
                {
                    { "Authorization", $"Bearer {token}" }
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

#pragma warning disable 8618
        private class AuthConfig
        {
            [JsonPropertyName("data")] public AuthConfigData Data { get; set; }
        }

        private class AuthConfigData
        {
            [JsonPropertyName("refresh_tokens")] public IReadOnlyCollection<AuthConfigRefreshToken> RefreshTokens { get; set; }
        }

        private class AuthConfigRefreshToken
        {
            [JsonPropertyName("id")] public string Id { get; set; }

            [JsonPropertyName("jwt_key")] public string Key { get; set; }
        }
    }
}