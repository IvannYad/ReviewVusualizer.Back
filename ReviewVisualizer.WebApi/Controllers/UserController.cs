using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.Linq.Expressions;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize(Policy = Policies.RequireOwner)]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserController> _logger;
        private static object _lock = new object();

        public UserController(ApplicationDbContext dbContext, ILogger<UserController> logger)
        {
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
            _logger = logger;
            _logger.LogInformation("[UserController] UserController instance created successfully");
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            _logger.LogInformation("[UserController] GetAll method called");

            try
            {
                _logger.LogDebug("[UserController] Retrieving all users with claims from database");

                var users = _dbContext.Users.Include(u => u.Claims)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        SystemRole = u.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.SystemRole.GetClaimType()),
                        GeneratorModification = u.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.GeneratorModifications.GetClaimType()),
                    })
                    .AsEnumerable()
                    .Select(x => new UserDto
                    {
                        UserId = x.Id,
                        UserName = x.UserName,
                        SystemRoles = Enum.TryParse<SystemRoles>(x.SystemRole?.ClaimValue ?? "0", out var role) ? role : SystemRoles.None,
                        GeneratorModifications = Enum.TryParse<GeneratorModifications>(x.GeneratorModification?.ClaimValue ?? "0", out var modification) ? modification : GeneratorModifications.View
                    })
                    .ToList();

                _logger.LogInformation("[UserController] Successfully retrieved {UserCount} users", users.Count);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserController] Error occurred while retrieving all users");
                throw;
            }
        }

        [HttpPatch("system-roles")]
        public IActionResult UpdateUserSystemRoles([FromBody] UpdateSystemRolesDto dto)
        {
            _logger.LogInformation("[UserController] UpdateUserSystemRoles method called for user {UserId} with roles {Roles}", dto.UserId, dto.Roles);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[UserController] ModelState validation failed for UpdateUserSystemRoles: {ValidationErrors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogDebug("[UserController] Looking up user {UserId} in database", dto.UserId);

                var user = _dbContext.Users.Include(u => u.Claims).FirstOrDefault(u => u.Id == dto.UserId);

                if (user is null)
                {
                    _logger.LogWarning("[UserController] User {UserId} not found for system roles update", dto.UserId);
                    return NotFound();
                }

                _logger.LogDebug("[UserController] User {UserId} ({UserName}) found, updating system roles", dto.UserId, user.UserName);

                var claim = user.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.SystemRole.GetClaimType());
                if (claim is null)
                {
                    _logger.LogDebug("[UserController] Adding new system role claim for user {UserId}", dto.UserId);
                    user.Claims.Add(new UserClaim
                    {
                        ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                        ClaimValue = Convert.ToInt32(dto.Roles).ToString()
                    });
                }
                else
                {
                    _logger.LogDebug("[UserController] Updating existing system role claim for user {UserId} from {OldValue} to {NewValue}",
                        dto.UserId, claim.ClaimValue, Convert.ToInt32(dto.Roles).ToString());
                    claim.ClaimValue = Convert.ToInt32(dto.Roles).ToString();
                }

                _dbContext.SaveChanges();
                _logger.LogInformation("[UserController] Successfully updated system roles for user {UserId} ({UserName}) to {Roles}",
                    dto.UserId, user.UserName, dto.Roles);

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserController] Error occurred while updating system roles for user {UserId}", dto.UserId);
                throw;
            }
        }

        [HttpPatch("generator-modifications")]
        public IActionResult UpdateUserGeneratorModifications([FromBody] UpdateGeneratorModificationsDto dto)
        {
            _logger.LogInformation("[UserController] UpdateUserGeneratorModifications method called for user {UserId} with modifications {Modifications}",
                dto.UserId, dto.Modifications);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[UserController] ModelState validation failed for UpdateUserGeneratorModifications: {ValidationErrors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogDebug("[UserController] Looking up user {UserId} in database", dto.UserId);

                var user = _dbContext.Users.FirstOrDefault(u => u.Id == dto.UserId);

                if (user is null)
                {
                    _logger.LogWarning("[UserController] User {UserId} not found for generator modifications update", dto.UserId);
                    return NotFound();
                }

                _logger.LogDebug("[UserController] User {UserId} ({UserName}) found, updating generator modifications", dto.UserId, user.UserName);

                var claim = user.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.GeneratorModifications.GetClaimType());
                if (claim is null)
                {
                    _logger.LogDebug("[UserController] Adding new generator modifications claim for user {UserId}", dto.UserId);
                    user.Claims.Add(new UserClaim
                    {
                        ClaimType = ClaimTypes.GeneratorModifications.GetClaimType(),
                        ClaimValue = Convert.ToInt32(dto.Modifications).ToString()
                    });
                }
                else
                {
                    _logger.LogDebug("[UserController] Updating existing generator modifications claim for user {UserId} from {OldValue} to {NewValue}",
                        dto.UserId, claim.ClaimValue, Convert.ToInt32(dto.Modifications).ToString());
                    claim.ClaimValue = Convert.ToInt32(dto.Modifications).ToString();
                }

                _dbContext.SaveChanges();
                _logger.LogInformation("[UserController] Successfully updated generator modifications for user {UserId} ({UserName}) to {Modifications}",
                    dto.UserId, user.UserName, dto.Modifications);

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserController] Error occurred while updating generator modifications for user {UserId}", dto.UserId);
                throw;
            }
        }

        [HttpDelete()]
        public IActionResult DeleteUser([FromQuery] int userId)
        {
            _logger.LogInformation("[UserController] DeleteUser method called for user {UserId}", userId);

            lock (_lock)
            {
                _logger.LogDebug("[UserController] Acquired lock for user deletion operation");

                try
                {
                    _logger.LogDebug("[UserController] Looking up user {UserId} in database", userId);

                    User? user = _dbContext.Users.FirstOrDefault(t => t.Id == userId);
                    if (user is null)
                    {
                        _logger.LogWarning("[UserController] User {UserId} not found for deletion", userId);
                        return NotFound();
                    }

                    _logger.LogDebug("[UserController] User {UserId} ({UserName}) found, proceeding with deletion", userId, user.UserName);

                    lock (_lock)
                    {
                        _logger.LogDebug("[UserController] Acquired inner lock for database operation");

                        _dbContext.Users.Remove(user);
                        _dbContext.SaveChanges();

                        _logger.LogInformation("[UserController] Successfully deleted user {UserId} ({UserName})", userId, user.UserName);
                    }

                    return Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[UserController] Error occurred while deleting user {UserId}", userId);
                    throw;
                }
            }
        }
    }
}