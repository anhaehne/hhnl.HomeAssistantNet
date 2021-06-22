using System;
using System.Threading.Tasks;
using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using Microsoft.AspNetCore.Mvc;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("api/build")]
    public class BuildController : Controller
    {
        private readonly IBuildService _buildService;

        public BuildController(IBuildService buildService)
        {
            _buildService = buildService;
        }

        [HttpPost("deploy")]
        public async Task<ActionResult> Deploy()
        {
            try
            {
                await _buildService.BuildAndDeployAsync();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

            return Ok();
        }
    }
}