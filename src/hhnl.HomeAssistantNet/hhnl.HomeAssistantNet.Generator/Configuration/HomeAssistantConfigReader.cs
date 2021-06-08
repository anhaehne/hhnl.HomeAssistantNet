using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace hhnl.HomeAssistantNet.Generator.Configuration
{
    public static class HomeAssistantConfigReader
    {
        public const string ConfigFileName = "ha-config.json";
        private const string TokenEnvironmentVariableName = "SUPERVISOR_TOKEN";
        private const string InstanceEnvironmentVariableName = "HOME_ASSISTANT_API";
        private const string InstanceEnvironmentVariableDefault = "http://supervisor/core/api";

        public static readonly DiagnosticDescriptor HaConfigNotFoundError = new("HHNLHAN001",
            "Home assistant configuration not found",
            "Unable to retrieve home assistant configuration. When using a ha-config.json file, make sure the BuildAction is set to 'AdditionalFiles'."
            ,
            "Configuration",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor _haConfigInvalidError = new("HHNLHAN002",
            "ha-config.json invalid",
            "The file ha-config.json is invalid. {0}",
            "Configuration",
            DiagnosticSeverity.Error,
            true);

        public static bool TryGetConfig(
            [NotNullWhen(true)] out HomeAssistantConfig? config,
            [NotNullWhen(false)] out Diagnostic? diagnostic)
        {
            // Try read from file
            if (TryGetConfigFile(out var configFile))
                return TryReadJsonConfiguration(configFile, out config, out diagnostic);

            if (TryReadEnvironmentConfiguration(out config, out diagnostic))
                return true;

            // No config found
            config = null;
            diagnostic = Diagnostic.Create(HaConfigNotFoundError, Location.None);
            return false;
        }

        public static bool TryReadEnvironmentConfiguration(
            [NotNullWhen(true)] out HomeAssistantConfig? config,
            out Diagnostic? diagnostic)
        {
            // Try read from environment
            var environmentToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariableName);

            if (string.IsNullOrEmpty(environmentToken))
            {
                config = null;
                diagnostic = null;
                return false;
            }

            config = new HomeAssistantConfig
            {
                Instance = Environment.GetEnvironmentVariable(InstanceEnvironmentVariableName) ??
                           InstanceEnvironmentVariableDefault,
                Token = environmentToken!
            };
            diagnostic = null;
            return true;
        }

        public static bool TryReadJsonConfiguration(
            string? configText,
            out HomeAssistantConfig? config,
            out Diagnostic? diagnostic)
        {
            if (string.IsNullOrEmpty(configText))
            {
                config = null;
                diagnostic = Diagnostic.Create(_haConfigInvalidError, Location.None, "The file is empty.");
                return false;
            }

            try
            {
                config = JsonConvert.DeserializeObject<HomeAssistantConfig>(configText!);
            }
            catch (JsonException e)
            {
                config = null;
                diagnostic = Diagnostic.Create(_haConfigInvalidError, Location.None, e.Message);
                return false;
            }

            if (config is null)
            {
                diagnostic = Diagnostic.Create(_haConfigInvalidError, Location.None, "Config value is null.");
                return false;
            }

            if (string.IsNullOrEmpty(config.Instance))
            {
                config = null;
                diagnostic = Diagnostic.Create(_haConfigInvalidError, Location.None, "instance is empty");
                return false;
            }

            if (string.IsNullOrEmpty(config.Token))
            {
                config = null;
                diagnostic = Diagnostic.Create(_haConfigInvalidError, Location.None, "token is empty");
                return false;
            }

            diagnostic = null;
            return true;
        }

        private static bool TryGetConfigFile([NotNullWhen(true)] out string? content)
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly is null)
            {
                content = null;
                return false;
            }

            var entryDirectory = Path.GetDirectoryName(entryAssembly.Location);

            if (entryDirectory is null)
            {
                content = null;
                return false;
            }

            var configFilePath = Path.Combine(entryDirectory, ConfigFileName);

            if (!File.Exists(configFilePath))
            {
                content = null;
                return false;
            }

            content = File.ReadAllText(configFilePath);
            return true;
        }
    }
}