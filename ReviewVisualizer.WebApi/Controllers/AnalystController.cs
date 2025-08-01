using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.AuthLibrary;

namespace ReviewVisualizer.Generator.Controllers
{
    [ApiController]
    [Route("analysts")]
    [Authorize(Policy = Policies.RequireAnalyst)]
    public class AnalystController : ControllerBase
    {
        [HttpGet("try-access")]
        public IActionResult TryAccess()
        {
            return Ok();
        }
    }
}
