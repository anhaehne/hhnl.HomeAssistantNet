using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using Microsoft.AspNetCore.Mvc;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildController : Controller
    {
        private readonly IBuildService _buildService;

        public BuildController(IBuildService buildService)
        {
            _buildService = buildService;
        }

        [HttpPost("start-deploy")]
        public async Task<ActionResult<Guid>> StartDeploy()
        {
            try
            {
                return await _buildService.StartBuildAndDeployAsync();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost("wait-for-deploy")]
        public async Task<ActionResult> WaitForDeploy()
        {
            try
            {
                await _buildService.WaitForBuildAndDeployAsync();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return Ok();
        }
    }
}