using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Automation;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("automation")]
    public class AutomationController : Controller
    {
        private readonly IAutomationsHostService _hostService;

        public AutomationController(IAutomationsHostService hostService)
        {
            _hostService = hostService;
        }
        
        [HttpGet]
        public Task<ActionResult<IReadOnlyCollection<AutomationInfoDto>>> GetAutomationsAsync()
        {
            return CallHostService(service => service.GetAutomationsAsync()!);
        }

        [HttpPost("{name}/start")]
        public Task<ActionResult<AutomationInfoDto>> StartAutomationAsync([FromRoute]string name)
        {
            return CallHostService(service => service.StartAutomationAsync(name));
        }
        
        [HttpPost("{name}/stop")]
        public Task<ActionResult<AutomationInfoDto>> StopAutomationAsync([FromRoute]string name, [FromBody]TimeSpan? timeout = null)
        {
            return CallHostService(service => service.StopAutomationAsync(name, timeout ?? TimeSpan.FromSeconds(30)));
        }

        private async Task<ActionResult<T>> CallHostService<T>(Func<IAutomationsHostService, Task<T?>> serviceCalls)
        {
            T? result;

            try
            {
                result = await serviceCalls(_hostService);
            }
            catch (TaskCanceledException)
            {
                return Problem("The automations host didn't respond in time.", statusCode: (int)HttpStatusCode.RequestTimeout);
            }
            catch (NoAutomationHostConnectedException)
            {
                return Problem("No automation host is connected.", statusCode: (int)HttpStatusCode.FailedDependency);
            }

            if (result is null)
                return Problem("No automation found with the given name.", statusCode: (int)HttpStatusCode.FailedDependency);
            
            return Ok(result);
        }
    }
}