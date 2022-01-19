using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Requests;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using hhnl.HomeAssistantNet.Shared.Configuration;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services
{
    public interface IBuildService
    {
        Task<Guid> StartBuildAndDeployAsync();
        Task WaitForBuildAndDeployAsync();
        void RunDeployedApplication();
        void StopDeployedApplication();
    }

    public class BuildService : IBuildService
    {
        private readonly IOptions<SupervisorConfig> _config;
        private readonly ILogger<BuildService> _logger;
        private readonly IOptions<HomeAssistantConfig> _haConfig;
        private readonly IManagementHubCallService _managementHubCallService;
        private readonly ISecretsService _secretsService;
        private readonly IHubContext<SupervisorApiHub, ISupervisorApiClient> _supervisorHub;
        private readonly IMediator _mediator;
        private Process? _automationProcess;

        private Task _buildAndDeployTask = Task.CompletedTask;
        private Guid _currentBuildLogId = Guid.Empty;

        private static readonly Regex _automationRefRegex = new("<\\s*(ProjectReference|PackageReference)\\s*Include\\s*=\\s*\".*hhnl\\.HomeAssistantNet\\.Automations.*\"");


        public BuildService(
            IOptions<SupervisorConfig> config, 
            IMediator mediator, 
            ILogger<BuildService> logger, 
            IOptions<HomeAssistantConfig> haConfig, 
            IManagementHubCallService managementHubCallService,
            ISecretsService secretsService,
            IHubContext<SupervisorApiHub, ISupervisorApiClient> supervisorHub)
        {
            _config = config;
            _mediator = mediator;
            _logger = logger;
            _haConfig = haConfig;
            _managementHubCallService = managementHubCallService;
            _secretsService = secretsService;
            _supervisorHub = supervisorHub;
        }

        public Task WaitForBuildAndDeployAsync()
        {
            return _buildAndDeployTask;
        }

        public Task<Guid> StartBuildAndDeployAsync()
        {
            if (_config.Value.SuppressAutomationDeploy)
            {
                return Task.FromResult(Guid.Empty);
            }

            // Start a new build if no build is running.
            if (_buildAndDeployTask.IsCompleted)
            {
                _currentBuildLogId = Guid.NewGuid();
                _buildAndDeployTask = BuildAndDeployAsyncInternal();
            }

            return Task.FromResult(_currentBuildLogId);
        }

        public void RunDeployedApplication()
        {
            if (_config.Value.SuppressAutomationDeploy)
                return;

            StopDeployedApplication();

            var deployPath = Path.GetFullPath(_config.Value.DeployDirectory);
            var dllPath = Path.Combine(deployPath, $"{GetProjectFileName()}.dll");

            _logger.LogInformation($"Starting deployed application: '{dllPath}'");

            ProcessStartInfo? runStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{dllPath} Token={_haConfig.Value.SUPERVISOR_TOKEN} SupervisorUrl=http://localhost:20777",
                WorkingDirectory = deployPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _automationProcess = Process.Start(runStartInfo);
        }

        public void StopDeployedApplication() => _automationProcess?.Close();

        private async Task BuildAndDeployAsyncInternal()
        {
            try
            {
                _logger.LogDebug("Starting build and deploy");

                var srcDirectory = Path.GetFullPath(_config.Value.SourceDirectory);
                var buildDirectory = Path.GetFullPath(_config.Value.BuildDirectory);
                var deployDirectory = Path.GetFullPath(_config.Value.DeployDirectory);

                // Copy source to build directory
                if (Directory.Exists(buildDirectory))
                    Directory.Delete(buildDirectory, true);

                DirectoryCopy(srcDirectory, buildDirectory);

                // The src directory is the solution directory so we have to find the automation project.
                if (!TryFindAutomationProject(buildDirectory, out string? projectDirectory))
                {
                    _logger.LogError($"Unable to determine automation project. Make sure the project directly references hhnl.HomeAssistantNet.Automations. SourceDirectory: '{srcDirectory}'");
                    return;
                }

                if (_managementHubCallService.DefaultConnection is not null)
                {
                    _logger.LogDebug("Stopping connection");

                    await _mediator.Send(new StopProcessRequest(_managementHubCallService.DefaultConnection.Id));
                }

                _logger.LogDebug("Starting dotnet publish");

                ProcessStartInfo? buildStartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = projectDirectory,
                    FileName = "dotnet",
                    Arguments = $"publish --force -c Release -o {deployDirectory}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                var buildProcess = Process.Start(buildStartInfo);

                if (buildProcess is null)
                {
                    _logger.LogError($"Unable to start build process");
                    return;
                }

                var stdOutTask = Task.Factory.StartNew(async () =>
                {
                    while (!buildProcess.StandardOutput.EndOfStream)
                    {
                        var line = await buildProcess.StandardOutput.ReadLineAsync();

                        if(!string.IsNullOrEmpty(line))
                            await _supervisorHub.Clients.All.OnNewLogMessage(new Shared.Supervisor.LogMessageDto(_currentBuildLogId, line, 0, 0, false));
                    }

                }, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

                using CancellationTokenSource? cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(3));

                try
                {
                    await buildProcess.WaitForExitAsync(cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogError("Build timed out.");
                    return;
                }

                if (buildProcess.ExitCode != 0)
                {
                    _logger.LogError($"dotnet published finished with exit code {buildProcess.ExitCode}.");
                    return;
                }

                _secretsService.DeploySecretsFile();
            }
            finally
            {
                await _supervisorHub.Clients.All.OnNewLogMessage(new Shared.Supervisor.LogMessageDto(_currentBuildLogId, string.Empty, 0, 0, true));
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories().Where(d => !d.Name.StartsWith(".") && d.Name != "obj" && d.Name != "bin");

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            

            // Copy subdirectories
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }

        private bool TryFindAutomationProject(string sourceRoot, [NotNullWhen(true)] out string? automationProjectFolder)
        {
            automationProjectFolder = null;
            string[]? projectFiles = Directory.GetFiles(sourceRoot, "*.csproj", SearchOption.AllDirectories);

            if (!projectFiles.Any())
            {
                return false;
            }

            foreach (string? projectFile in projectFiles)
            {
                if (_automationRefRegex.IsMatch(File.ReadAllText(projectFile)))
                {
                    automationProjectFolder = new FileInfo(projectFile).DirectoryName!;
                    return true;
                }
            }

            return false;
        }

        private string GetProjectFileName()
        {
            string? srcDirectory = Path.GetFullPath(_config.Value.SourceDirectory);

            // The src directory is the solution directory so we have to find the automation project.
            if (!TryFindAutomationProject(srcDirectory, out string? projectDirectory))
            {
                throw new InvalidOperationException($"Unable to determine automation project. Make sure the project directly references hhnl.HomeAssistantNet.Automations. SourceDirectory: '{srcDirectory}'");
            }

            string? projectPath = Directory.EnumerateFiles(projectDirectory, "*.csproj").Single();

            FileInfo? fileInfo = new FileInfo(projectPath);

            return fileInfo.Name.Replace(fileInfo.Extension, "");
        }
    }



    public record BuildContext(Guid LogId, IHubContext<SupervisorApiHub, ISupervisorApiClient> HubContext);

    public class BuildServiceLogger : ILogger
    {
        private static AsyncLocal<BuildContext?> _buildContext = new AsyncLocal<BuildContext?>();

        public static BuildContext? BuildContext
        {
            get {  return _buildContext.Value; }
            set {  _buildContext.Value = value; }
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => BuildContext is not null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {

        }
    }
}