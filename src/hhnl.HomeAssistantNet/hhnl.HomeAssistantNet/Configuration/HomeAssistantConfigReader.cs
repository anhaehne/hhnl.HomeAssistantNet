using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace hhnl.HomeAssistantNet.Configuration
{
    public static class HomeAssistantConfigReader
    {
        private const string ConfigFileName = "ha-config.json";
        private const string TokenEnvironmentVariableName = "SUPERVISOR_TOKEN";
        private const string InstanceEnvironmentVariableName = "HOME_ASSISTANT_API";
        private const string InstanceEnvironmentVariableDefault = "http://supervisor/core/api";
        
        private static readonly DiagnosticDescriptor _haConfigNotFoundError = new ("HHNLHAN001",
            title: "Home assistant configuration not found",
            messageFormat: "Unable to retrieve home assistant configuration. When using a ha-config.json file, make sure the BuildAction is set to 'AdditionalFiles'.",
            category: "Configuration",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        private static readonly DiagnosticDescriptor _haConfigInvalidError = new ("HHNLHAN002",
            title: "ha-config.json invalid",
            messageFormat: "The file ha-config.json is invalid. {0}",
            category: "Configuration",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        public static bool TryGetConfig(
            IReadOnlyCollection<AdditionalText> additionalFiles,
            [NotNullWhen(true)] out HomeAssistantConfig? config,
            [NotNullWhen(false)] out Diagnostic? diagnostic,
            CancellationToken cancellationToken)
        {
            // Try read from file
            var configFile = additionalFiles.SingleOrDefault(f => new FileInfo(f.Path).Name == ConfigFileName);

            if (configFile is not null)
                return TryReadJsonConfiguration(configFile, out config, out diagnostic, cancellationToken);

            // Try read from environment
            var environmentToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariableName);

            if (!string.IsNullOrEmpty(environmentToken))
            {
                config =  new HomeAssistantConfig
                {
                    Instance = Environment.GetEnvironmentVariable(InstanceEnvironmentVariableName) ?? InstanceEnvironmentVariableDefault,
                    Token = environmentToken!,
                };
                diagnostic = null;
                return true;
            }

            // No config found
            config = null;
            diagnostic = Diagnostic.Create(_haConfigNotFoundError, Location.None);
            return false;
        }

        private static bool TryReadJsonConfiguration(
            AdditionalText configFile,
            out HomeAssistantConfig? config,
            out Diagnostic? diagnostic,
            CancellationToken cancellationToken)
        {
            var configText = configFile.GetText(cancellationToken)?.ToString();

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
    }
}