using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data.Dto;
using LoginRequest = ReviewVisualizer.Data.Dto.LoginRequest;
using RegisterRequest = ReviewVisualizer.Data.Dto.RegisterRequest;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> LogInAsync([FromBody] LoginRequest loginRequest)
        {
            return Ok(new LoginResponse(true));
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            return Ok(new RegisterResponse(true));
        }

        [HttpPost("logoff")]
        public async Task<IActionResult> LogoffAsync([FromBody] LogoffRequest logoffRequest)
        {
            return Ok(new LogoffResponse(true));
        }
    }
}
