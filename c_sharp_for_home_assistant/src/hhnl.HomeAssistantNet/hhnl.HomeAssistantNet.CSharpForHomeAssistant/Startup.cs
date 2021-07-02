using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Notifications;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Configuration;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

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
            // Dependencies
            services.AddSignalR();
            services.AddControllers();
            services.AddMediatR(typeof(Startup));
            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddRazorPages();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.ForwardLimit = 1;
            });

            // Make sure this is the first hosted service
            services.AddHostedService<InitService>();

            services.Configure<SupervisorConfig>(Configuration.GetSection(nameof(SupervisorConfig)));
            services.Configure<HomeAssistantConfig>(Configuration);
            services.PostConfigure<HomeAssistantConfig>(config =>
            {
                // When not configured otherwise we expect to run in a Home Assistant Add-ons.
                config.HOME_ASSISTANT_API ??= "http://supervisor/core/";
            });

            services.AddSingleton<IAutomationsHostService, AutomationsService>();
            services.AddSingleton<IBuildService, BuildService>();
            services.AddSingleton<IManagementHubCallService, ManagementHubCallService>();
            services.AddSingleton<IHomeAssistantTokenValidationService, HomeAssistantTokenValidationService>();

            services.AddSingleton<NotificationQueue>();
            services.AddSingleton<IHostedService>(s => s.GetRequiredService<NotificationQueue>());
            services.AddSingleton<INotificationQueue>(s => s.GetRequiredService<NotificationQueue>());

            services.AddSingleton<ISecretsService, SecretsService>();

            // These notifcations will populate the notification queue on startup.
            services.AddSingleton<INotification>(NoConnectionNotification.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
                app.UseExceptionHandler("/Home/Error");

            app.UseMiddleware<IngressPathMiddleWare>();
            app.UseMiddleware<AcceptEncodingMiddleware>();

            app.UseForwardedHeaders();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseMiddleware<AuthenticationMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<ManagementHub>("api/client-management");
                endpoints.MapHub<SupervisorApiHub>("api/supervisor-api");
                endpoints.MapControllers();
                endpoints.MapFallback(ServeIndexHtml);
            });
        }

        private const string XIngressPathHeaderName = "X-Ingress-Path";
        private static readonly Regex _basePathRegex = new Regex("(<base\\s*href=\")\\/(\"\\s*\\/>)"); 
        
        private async Task ServeIndexHtml(HttpContext context)
        {
            var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

            if (cache.TryGetValue<byte[]>($"X-Ingress-Path_{context.Request.Headers[XIngressPathHeaderName]}",
                out var cachedFile))
            {
                await WriteFileAsync(context, cachedFile);
                return;
            }

            var sourceFile = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo("index.html");
            
            await using var fs = sourceFile.CreateReadStream();
            using var reader = new StreamReader(fs);

            var content = await reader.ReadToEndAsync();

            if (context.Request.Headers[XIngressPathHeaderName] != StringValues.Empty)
            {
                var basePath = context.Request.Headers[XIngressPathHeaderName].ToString();

                if (!basePath.EndsWith("/"))
                    basePath += "/";
                
                content = _basePathRegex.Replace(content, $"$1{basePath}$2");
            }

            var patchedFile = Encoding.UTF8.GetBytes(content);
            cache.Set($"X-Ingress-Path_{context.Request.Headers[XIngressPathHeaderName]}", patchedFile);

            await WriteFileAsync(context, patchedFile);
            
        }
        
        private static async Task WriteFileAsync(HttpContext context, ReadOnlyMemory<byte> buffer)
        {
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentLength = buffer.Length;

            await using var stream = context.Response.Body;

            await stream.WriteAsync(buffer);
            await stream.FlushAsync();
        }
    }
}