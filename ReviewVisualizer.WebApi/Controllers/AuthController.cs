using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.AuthLibrary.Exceptions;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.WebApi.Services;
using LoginRequest = ReviewVisualizer.Data.Dto.LoginRequest;
using RegisterRequest = ReviewVisualizer.Data.Dto.RegisterRequest;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogInAsync([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest is null)
                return BadRequest("Login data is not provided");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var principal = await _authService.LoginAsync(loginRequest);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    });

                return Ok(new LoginResponse(true));
            }
            catch (UserUnauthenticatedException ex)
            {
                return Unauthorized(new LoginResponse(false, ex.Message));
            }
            catch (Exception ex)
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected server error ocurred",
                    detail: ex.Message
                );
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest is null)
                return BadRequest("Register data is not provided");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var registerResponse = await _authService.RegisterAsync(registerRequest);

                return Ok(new RegisterResponse(registerResponse));
            }
            catch (UserAlreadyExistsException ex)
            {
                return BadRequest(new RegisterResponse(false, registerRequest.Username, ex.Message));
            }
            catch (Exception ex)
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected server error ocurred",
                    detail: ex.Message
                );
            }
        }

        [HttpPost("logoff")]
        public async Task<IActionResult> LogoffAsync([FromBody] LogoffRequest logoffRequest)
        {
            return Ok(new LogoffResponse(true));
        }
    }
}
