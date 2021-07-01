using hhnl.HomeAssistantNet.CSharpForHomeAssistant.Services;
using hhnl.HomeAssistantNet.Shared.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecretsController : Controller
    {
        private readonly ISecretsService _secretsService;

        public SecretsController(ISecretsService secretsService)
        {
            _secretsService = secretsService;
        }

        [HttpGet]
        public async Task<ActionResult<AutomationSecrets>> GetAsync()
        {
            return await _secretsService.GetSecretsAsync();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(AutomationSecrets secrets)
        {
            await _secretsService.SaveSecretsAsync(secrets);
            return Ok();
        }
    }
}
