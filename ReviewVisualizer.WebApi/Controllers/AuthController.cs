using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.AuthLibrary;
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
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogInAsync([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest is null)
                return BadRequest("Login data is not provided");

            try
            {
                var principal = await _authService.LoginAsync(loginRequest).ConfigureAwait(false);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    }).ConfigureAwait(false);

                HttpContext.Response.Cookies.Append(
                    "UserName",
                    loginRequest.Username,
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddHours(1),
                        HttpOnly = false,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        IsEssential = true // important if you use cookie consent
                    });

                return Ok(new LoginResponse(true));
            }
            catch (UserUnauthenticatedException ex)
            {
                return Unauthorized(new LoginResponse(false, ex.Message));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            if (registerRequest is null)
                return BadRequest("Register data is not provided");

            try
            {
                var registerResponse = await _authService.RegisterAsync(registerRequest).ConfigureAwait(false);

                return Ok(new RegisterResponse(registerResponse, registerRequest.Username, registerRequest.Password));
            }
            catch (UserAlreadyExistsException ex)
            {
                return BadRequest(new RegisterResponse(false, registerRequest.Username, registerRequest.Password, ex.Message));
            }
        }

        [HttpPost("logoff")]
        public async Task<IActionResult> LogoffAsync([FromBody] LogoffRequest logoffRequest)
        {
            if (logoffRequest is null)
                return BadRequest("Logoff data is not provided");

            // Remove cusotm cookie "UserName"
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie, new CookieOptions
                {
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                });
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return NoContent();
        }

        [HttpGet("try-visitor-access")]
        [Authorize(Policy = Policies.RequireVisitor)]
        public IActionResult TryVisitorAccess()
        {
            return Ok();
        }

        [HttpGet("try-analyst-access")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult TryAnalystAccess()
        {
            return Ok();
        }

        [HttpGet("try-generator-access")]
        [Authorize(Policy = Policies.RequireGeneratorAdmin)]
        public IActionResult TryGeneratorAccess()
        {
            return Ok();
        }

        [HttpGet("try-owner-access")]
        [Authorize(Policy = Policies.RequireOwner)]
        public IActionResult TryOwnerAccess()
        {
            return Ok();
        }
    }
}