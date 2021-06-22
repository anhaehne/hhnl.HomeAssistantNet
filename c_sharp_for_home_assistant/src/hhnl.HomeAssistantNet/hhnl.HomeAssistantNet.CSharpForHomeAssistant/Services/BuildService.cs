using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.Shared.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IBuildService
    {
        Task<bool> BuildAndDeployAsync();
        Task WaitForBuildAndDeployAsync();
        void RunDeployedApplication();
    }

    public class BuildService : IBuildService
    {
        private readonly IOptions<SupervisorConfig> _config;
        private readonly ILogger<BuildService> _logger;
        private readonly IOptions<HomeAssistantConfig> _haConfig;
        private readonly IMediator _mediator;
        private Task<bool> _buildAndDeployTask = Task.FromResult(true);


        public BuildService(IOptions<SupervisorConfig> config, IMediator mediator, ILogger<BuildService> logger, IOptions<HomeAssistantConfig> haConfig)
        {
            _config = config;
            _mediator = mediator;
            _logger = logger;
            _haConfig = haConfig;
        }

        public Task WaitForBuildAndDeployAsync()
        {
            return _buildAndDeployTask;
        }

        public Task<bool> BuildAndDeployAsync()
        {
            if (_config.Value.SuppressAutomationDeploy)
                return Task.FromResult(true);

            _buildAndDeployTask = BuildAndDeployAsyncInternal();
            return _buildAndDeployTask;
        }

        public void RunDeployedApplication()
        {
            if (_config.Value.SuppressAutomationDeploy)
                return;
            
            var dllPath = Path.Combine(Path.GetFullPath(_config.Value.DeployDirectory), $"{GetProjectFileName()}.dll");

            _logger.LogInformation($"Starting deployed application: '{dllPath}'");
            
            var runStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{dllPath} Token={_haConfig.Value.SUPERVISOR_TOKEN} SupervisorUrl=http://localhost:20777",
            };
            var runProcess = Process.Start(runStartInfo);
        }

        private async Task<bool> BuildAndDeployAsyncInternal()
        {
            _logger.LogDebug("Starting build and deploy");

            var srcDirectory = Path.GetFullPath(_config.Value.SourceDirectory);


            _logger.LogDebug("Stopping all processes");

            await _mediator.Send(StopProcessesRequest.All);

            _logger.LogDebug("Starting dotnet publish");

            var buildStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = srcDirectory, FileName = "dotnet",
                Arguments = $"publish --force -c Release -o {Path.GetFullPath(_config.Value.DeployDirectory)}"
            };

            var buildProcess = Process.Start(buildStartInfo);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            try
            {
                await buildProcess!.WaitForExitAsync(cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Build timed out.");
                return false;
            }

            if (buildProcess.ExitCode != 0)
            {
                _logger.LogError($"dotnet published finished with exit code {buildProcess.ExitCode}.");
                return false;
            }

            // Delete the bin and obj folder so the next time the users opens the project they don't get an error due to missing nuget packages.
            DeleteFolder("bin");
            DeleteFolder("obj");
            
            return true;
        }

        private void DeleteFolder(string folder)
        {
            var path = Path.Combine(Path.GetFullPath(_config.Value.SourceDirectory), folder);
            if(Directory.Exists(path))
                Directory.Delete(path, true);
        }
        
        private string GetProjectFileName()
        {
            var srcDirectory = Path.GetFullPath(_config.Value.SourceDirectory);

            var projectPath = Directory.EnumerateFiles(srcDirectory, "*.csproj").Single();

            var fileInfo = new FileInfo(projectPath);

            return fileInfo.Name.Replace(fileInfo.Extension, "");
        }
    }
}