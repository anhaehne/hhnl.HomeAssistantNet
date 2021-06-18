using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public class InitService : IHostedService
    {
        private readonly ILogger<InitService> _logger;
        private readonly IOptions<SupervisorConfig> _config;
        private readonly IBuildService _buildService;

        public InitService(ILogger<InitService> logger, IOptions<SupervisorConfig> config, IBuildService buildService)
        {
            _logger = logger;
            _config = config;
            _buildService = buildService;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await SetupAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task SetupAsync()
        {
            if (_config.Value.SuppressAutomationDeploy)
                return;
            
            // TODO: Handle failures
            var sourceFolder = new DirectoryInfo(_config.Value.SourceDirectory);
            sourceFolder.Create();

            if (!sourceFolder.EnumerateFiles().Any())
            {
                _logger.LogInformation("No source files found. Copying template.");
                CopyFiles("/app/ProjectTemplate", sourceFolder.FullName);
            }
            
            var deployFolder = new DirectoryInfo(_config.Value.DeployDirectory);
            deployFolder.Create();

            if (!deployFolder.EnumerateFiles().Any())
            {
                _logger.LogInformation("Running initial build.");
                await _buildService.BuildAndDeployAsync();
            }
        }
        
        private static void CopyFiles(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
    }
}