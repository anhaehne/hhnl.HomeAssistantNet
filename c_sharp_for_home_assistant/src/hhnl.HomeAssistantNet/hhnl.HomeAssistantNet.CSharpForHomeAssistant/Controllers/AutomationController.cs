using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Supervisor;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public Task<ActionResult<AutomationInfoDto>> StartAutomationAsync([FromRoute] string name)
        {
            return CallHostService(service => service.StartAutomationAsync(name));
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
                return Problem("No automation found with the given name.", statusCode: (int)HttpStatusCode.NotFound);

            return Ok(result);
        }
    }
}