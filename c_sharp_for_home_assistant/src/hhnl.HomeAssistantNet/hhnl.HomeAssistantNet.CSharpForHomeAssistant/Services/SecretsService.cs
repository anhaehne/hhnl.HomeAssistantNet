using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface ISecretsService
    {
        void DeploySecretsFile();
        Task<AutomationSecrets> GetSecretsAsync();
        Task SaveSecretsAsync(AutomationSecrets secrets);
    }

    public class SecretsService : ISecretsService
    {
        private readonly string _secretsConfigPath;
        private readonly string _secretsDeployPath;

        public SecretsService(IOptions<SupervisorConfig> config)
        {
            Directory.CreateDirectory(Path.GetFullPath(config.Value.ConfigDirectory));
            Directory.CreateDirectory(Path.GetFullPath(config.Value.DeployDirectory));
            _secretsConfigPath = Path.Combine(Path.GetFullPath(config.Value.ConfigDirectory), "secrets.json");
            _secretsDeployPath = Path.Combine(Path.GetFullPath(config.Value.DeployDirectory), "secrets.json");
        }

        public async Task<AutomationSecrets> GetSecretsAsync()
        {
            if (!File.Exists(_secretsConfigPath))
                return AutomationSecrets.Empty;

            using var fs = File.OpenRead(_secretsConfigPath);

            var file = await JsonSerializer.DeserializeAsync<SecretsFile>(fs);

            if (file is null)
                return AutomationSecrets.Empty;

            return file.Secrets;
        }

        public async Task SaveSecretsAsync(AutomationSecrets secrets)
        {
            var file = new SecretsFile(secrets);

            if (File.Exists(_secretsConfigPath))
                File.Delete(_secretsConfigPath);

            // Write the config file and copy it to the deploy directory.
            using(var fs = File.OpenWrite(_secretsConfigPath))
            {
                await JsonSerializer.SerializeAsync(fs, file);
            }

            DeploySecretsFile();
        }

        public void DeploySecretsFile()
        {
            if(File.Exists(_secretsConfigPath))
                File.Copy(_secretsConfigPath, _secretsDeployPath, true);
        }

        private class SecretsFile
        {
            public AutomationSecrets Secrets { get; }

            [JsonConstructor]
            public SecretsFile(AutomationSecrets secrets)
            {
                Secrets = secrets;
            }
        }
    }
}
