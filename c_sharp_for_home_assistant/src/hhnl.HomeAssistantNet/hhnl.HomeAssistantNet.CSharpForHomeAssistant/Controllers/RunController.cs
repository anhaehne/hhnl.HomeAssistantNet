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
    public class RunController : ControllerBase
    {
        private readonly IManagementHubCallService _managementHubCallService;

        public RunController(IManagementHubCallService managementHubCallService)
        {
            _managementHubCallService = managementHubCallService;
        }

        [HttpPost("{runId}/logs/start-listen")]
        public async Task<ActionResult<IReadOnlyCollection<LogMessageDto>>> LogStartListen([FromRoute] Guid runId)
        {
            var result = await CallHostService(client => client.CallService<IReadOnlyCollection<LogMessageDto>>((messageId, client) =>
                client.StartListenToRunLog(messageId, runId)
            ));

            return result;
        }

        [HttpPost("{runId}/logs/stop-listen")]
        public async Task<ActionResult<IReadOnlyCollection<LogMessageDto>>> StopStartListen([FromRoute] Guid runId)
        {
            await CallHostService(client => client.CallService<IReadOnlyCollection<LogMessageDto>>((messageId, client) =>
                client.StartListenToRunLog(messageId, runId)
            ));

            return Ok();
        }

        [HttpPost("{runId}/stop")]
        public async Task<ActionResult<AutomationInfoDto>> StopAutomationAsync([FromRoute] Guid runId)
        {
            await CallHostService(client => client.CallService<bool>((messageId, client) =>
                client.StopAutomationRunAsync(messageId, runId)
            ));

            return Ok();
        }

        private async Task<ActionResult<T>> CallHostService<T>(Func<IManagementHubCallService, Task<T?>> serviceCalls)
        {
            T? result;

            try
            {
                result = await serviceCalls(_managementHubCallService);
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
                return Problem("No run found with the given id.", statusCode: (int)HttpStatusCode.NotFound);

            return Ok(result);
        }
    }
}
