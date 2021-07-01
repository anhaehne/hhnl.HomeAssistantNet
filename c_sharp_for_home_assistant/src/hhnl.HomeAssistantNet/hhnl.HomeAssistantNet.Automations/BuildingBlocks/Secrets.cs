using hhnl.HomeAssistantNet.Automations.Automation;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace hhnl.HomeAssistantNet.Automations.BuildingBlocks
{
    public static class Secrets
    {
        public static bool HasSecret(string key)
        {
            return GetSecrets().ContainsKey(key);
        }

        public static string GetSecret(string key)
        {
            if (!HasSecret(key))
            {
                throw new ArgumentException($"No scecret with the key '{key}' found", nameof(key));
            }

            return GetSecrets()[key];
        }

        public static IReadOnlyDictionary<string, string> GetSecrets()
        {
            return AutomationRunContext.GetRunContextOrFail().ServiceProvider.GetRequiredService<IOptionsSnapshot<AutomationSecrets>>().Value;
        }
    }
}
