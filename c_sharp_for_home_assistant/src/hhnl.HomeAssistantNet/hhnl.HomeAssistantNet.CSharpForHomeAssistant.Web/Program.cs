using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Modal;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
            });
            
            builder.Services.AddBlazoredModal();

            builder.Services.AddScoped<SupervisorApiService>();
            builder.Services.AddScoped<AuthenticationService>();
            
            await builder.Build().RunAsync();
        }
    }
}