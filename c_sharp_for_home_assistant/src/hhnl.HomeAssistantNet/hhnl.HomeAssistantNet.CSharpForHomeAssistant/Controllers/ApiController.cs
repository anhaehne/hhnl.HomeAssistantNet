using Microsoft.AspNetCore.Mvc;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        /// <summary>
        /// Can be called to check authentication.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}