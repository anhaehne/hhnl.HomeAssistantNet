using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.Automations.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Automations.Supervisor;
using hhnl.HomeAssistantNet.Shared.Configuration;
using hhnl.HomeAssistantNet.Shared.Entities;
using hhnl.HomeAssistantNet.Shared.HomeAssistantConnection;
using hhnl.HomeAssistantNet.Shared.SourceGenerator;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var hostBuilder = Host.CreateDefaultBuilder(args);
            ConfigureHost(hostBuilder);
            return hostBuilder.Build().RunAsync();
        }

        private void ConfigureServiceInternal(HostBuilderContext builderContext, IServiceCollection services)
        {
            var metaData = GetGenerated();
            services.AddSingleton(metaData);
            metaData.RegisterEntitiesAndAutomations(services);

            services.Configure<HomeAssistantConfig>(builderContext.Configuration);
            services.PostConfigure<HomeAssistantConfig>(config =>
            {
                // When not configured otherwise we expect to run in a Home Assistant Add-ons.
                if (string.IsNullOrEmpty(config.Token))
                    config.Token = Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN") ?? string.Empty;
                
                if (string.IsNullOrEmpty(config.Instance))
                    config.Instance = Environment.GetEnvironmentVariable("HOME_ASSISTANT_API") ?? "http://supervisor/core/";
            });

            var t = builderContext.Configuration["SupervisorUrl"];
            
            services.Configure<AutomationsConfig>(builderContext.Configuration);

            // Order is important.
            services.AddSingleton<HomeAssistantClient>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<HomeAssistantClient>());
            services.AddSingleton<IHomeAssistantClient>(s => s.GetRequiredService<HomeAssistantClient>());
            
            services.AddSingleton<EntityRegistry>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<EntityRegistry>());
            services.AddSingleton<IEntityRegistry>(s => s.GetRequiredService<EntityRegistry>());
            
            services.AddSingleton<AutomationRunner>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<AutomationRunner>());
            services.AddSingleton<IAutomationRunner>(s => s.GetRequiredService<AutomationRunner>());
            
            services.AddSingleton<IAutomationRegistry, AutomationRegistry>();

            services.AddHostedService<SupervisorClient>();
            
            // Handler
            services
                .AddTransient<INotificationHandler<HomeAssistantClient.StateChangedNotification>,
                    StateChangedNotificationHandler>();
            
            // Dependencies
            services.AddMediatR(GetType());
        }

        protected virtual void ConfigureServices(HostBuilderContext builderContext, IServiceCollection serviceCollection)
        {
            // Empty on purpose.
        }

        protected void ConfigureHost(IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration(c => c.AddJsonFile("ha-config.json", true))
                .ConfigureServices(ConfigureServices)
                .ConfigureServices(ConfigureServiceInternal)
                
                #if DEBUG
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Debug))
                #endif
                
                .UseConsoleLifetime();
        }

        private IGeneratedMetaData GetGenerated()
        {
            var metaDataType = _assembly.GetTypes().Single(t => t.Name == GeneratorConstants.MetaDataClassName);

            return (IGeneratedMetaData)Activator.CreateInstance(metaDataType);
        }
    }
}