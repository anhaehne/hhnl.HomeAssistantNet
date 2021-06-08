using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.CodeAnalysis;

namespace hhnl.HomeAssistantNet.Generator.Configuration
{
    public static class HomeAssistantCompileTimeConfigReader
    {
        public static bool TryGetConfig(
            IReadOnlyCollection<AdditionalText> additionalFiles,
            [NotNullWhen(true)] out HomeAssistantConfig? config,
            [NotNullWhen(false)] out Diagnostic? diagnostic,
            CancellationToken cancellationToken)
        {
            // Try read from file
            var configFile =
                additionalFiles.SingleOrDefault(f => new FileInfo(f.Path).Name == HomeAssistantConfigReader.ConfigFileName);

            if (configFile is not null)
            {
                var text = configFile.GetText(cancellationToken)?.ToString();
                return HomeAssistantConfigReader.TryReadJsonConfiguration(text, out config, out diagnostic);
            }

            if (HomeAssistantConfigReader.TryReadEnvironmentConfiguration(out config, out diagnostic))
                return true;

            // No config found
            config = null;
            diagnostic = Diagnostic.Create(HomeAssistantConfigReader.HaConfigNotFoundError, Location.None);
            return false;
        }
    }
}