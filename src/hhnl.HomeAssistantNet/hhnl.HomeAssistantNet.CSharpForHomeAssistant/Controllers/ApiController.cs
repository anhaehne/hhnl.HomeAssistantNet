using System.Threading.Tasks;
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
        public async Task<ActionResult> GetAsync()
        {
            return Ok();
        }
    }
}