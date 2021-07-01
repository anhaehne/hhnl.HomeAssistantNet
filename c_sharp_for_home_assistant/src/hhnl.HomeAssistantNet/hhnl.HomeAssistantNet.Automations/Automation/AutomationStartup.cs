using hhnl.HomeAssistantNet.Automations.Automation.Runner;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Automations.Supervisor;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.Automations.Automation
{
    public abstract class AutomationStartup
    {
        private readonly Assembly _assembly;

        public AutomationStartup(Type? assemblyToSearchIn = null)
        {
            _assembly = assemblyToSearchIn?.Assembly ?? Assembly.GetEntryAssembly()!;
        }

        public Task RunAsync(string[] args)
        {
            IHostBuilder? hostBuilder = Host.CreateDefaultBuilder(args);
            ConfigureHost(hostBuilder);
            return hostBuilder.Build().RunAsync();
        }

        private void ConfigureServiceInternal(HostBuilderContext builderContext, IServiceCollection services)
        {
            IGeneratedMetaData? metaData = GetGenerated();
            services.AddSingleton(metaData);
            metaData.RegisterEntitiesAndAutomations(services);

            services.Configure<HomeAssistantConfig>(builderContext.Configuration);
            services.PostConfigure<HomeAssistantConfig>(config =>
            {
                // When not configured otherwise we expect to run in a Home Assistant Add-ons.
                config.HOME_ASSISTANT_API ??= "http://supervisor/core/";
            });

            services.Configure<AutomationsConfig>(builderContext.Configuration);
            services.Configure<AutomationSecrets>(builderContext.Configuration.GetSection("Secrets"));

            // Order is important.
            services.AddSingleton<HomeAssistantClient>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<HomeAssistantClient>());
            services.AddSingleton<IHomeAssistantClient>(s => s.GetRequiredService<HomeAssistantClient>());

            services.AddSingleton<EntityRegistry>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<EntityRegistry>());
            services.AddSingleton<IEntityRegistry>(s => s.GetRequiredService<EntityRegistry>());

            services.AddSingleton<AutomationService>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<AutomationService>());
            services.AddSingleton<IAutomationService>(s => s.GetRequiredService<AutomationService>());

            services.AddSingleton<IAutomationRegistry, AutomationRegistry>();
            services.AddSingleton<IAutomationRunnerFactory, AutomationRunnerFactory>();

            services.AddSingleton<SupervisorClient>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<SupervisorClient>());

            // Dependencies
            services.AddMediatR(typeof(AutomationStartup));
        }

        protected virtual void ConfigureServices(HostBuilderContext builderContext, IServiceCollection serviceCollection)
        {
            // Empty on purpose.
        }

        protected void ConfigureHost(IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, builder) =>
                {
                    Assembly? appAssembly = Assembly.Load(new AssemblyName(context.HostingEnvironment.ApplicationName));
                    builder.AddUserSecrets(appAssembly, optional: true);
                    builder.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices(ConfigureServices)
                .ConfigureServices(ConfigureServiceInternal)
                .ConfigureLogging(x => x.AddProvider(new AutomationLogger.Provider()))

#if DEBUG
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Debug))
#endif

                .UseConsoleLifetime();
        }

        private IGeneratedMetaData GetGenerated()
        {
            Type? metaDataType = _assembly.GetTypes().Single(t => t.Name == GeneratorConstants.MetaDataClassName);

            object? metaData = Activator.CreateInstance(metaDataType);

            if (metaData is IGeneratedMetaData data)
            {
                return data;
            }

            throw new InvalidOperationException("Generated meta data not found.");
        }
    }
}