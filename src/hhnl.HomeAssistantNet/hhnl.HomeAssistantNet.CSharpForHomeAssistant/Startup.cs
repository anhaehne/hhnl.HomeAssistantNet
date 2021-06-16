using System;
using System.IO;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Make sure this is the first hosted service
            services.AddHostedService<InitService>();

            services.Configure<SupervisorConfig>(Configuration.GetSection(nameof(SupervisorConfig)));

            services.AddSignalR();
            services.AddControllers();

            services.AddMediatR(typeof(Startup));

            services.AddSingleton<IAutomationsHostService, AutomationsService>();
            services.AddSingleton<IBuildService, BuildService>();
            services.AddSingleton<IHubCallService, HubCallService>();
            services.AddSingleton<IProcessManager, ProcessManager>();

            services.AddSingleton<NotificationQueue>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<NotificationQueue>());
            services.AddSingleton<INotificationQueue>(s => s.GetRequiredService<NotificationQueue>());

            services.AddSingleton<INotification>(NoConnectionNotification.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ManagementHub>("client-management");
                endpoints.MapControllers();
            });
        }
    }
}