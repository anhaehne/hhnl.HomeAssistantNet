using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using hhnl.HomeAssistantNet.Generator.SourceGenerator;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace hhnl.HomeAssistantNet.Generator.Configuration
{
    public static class HomeAssistantConfigReader
    { 
        public static readonly DiagnosticDescriptor HaConfigNotFoundError = new("HHNLHAN001",
            "Home assistant configuration not found",
            "Please check the documentation on how to setup local development: https://github.com/anhaehne/hhnl.HomeAssistantNet/tree/main/c_sharp_for_home_assistant"
            ,
            "Configuration",
            DiagnosticSeverity.Error,
            true);
        
        public static bool TryGetConfig(
            GeneratorExecutionContext context,
            [NotNullWhen(true)] out HomeAssistantConfig? config,
            [NotNullWhen(false)] out Diagnostic? diagnostic,
            CancellationToken cancellationToken)
        {
            var secretsId = GetUserSecretsId(context);
            
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddEnvironmentVariables();

            if (secretsId is not null)
                configBuilder.AddUserSecrets(secretsId);

            config = configBuilder.Build().Get<HomeAssistantConfig>();

            if (config?.SUPERVISOR_TOKEN is null)
            {
                diagnostic = Diagnostic.Create(HaConfigNotFoundError, Location.None);
                config = null;
                return false;
            }

            // When not configured otherwise we expect to run in a Home Assistant Add-ons.
            config.HOME_ASSISTANT_API ??= "http://supervisor/core/";

            diagnostic = null;
            return true;
        }

        private static string? GetUserSecretsId(GeneratorExecutionContext context)
        {
            var userSecretsAttribute = context.Compilation.Assembly.GetAttributes().SingleOrDefault(x => x.AttributeClass?.GetFullName(null) == typeof(UserSecretsIdAttribute).GetFullName());

            if (userSecretsAttribute is null)
                return null;
            
            return userSecretsAttribute.ConstructorArguments.First().Value?.ToString();
        }
    }
}