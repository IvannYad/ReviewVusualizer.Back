using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("users")]
    [Authorize(Policy = Policies.RequireOwner)]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private static object _lock = new object();

        public UserController(ApplicationDbContext dbContext)
        {
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
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

            return Ok(users);
        }

        [HttpPatch("system-roles")]
        public IActionResult UpdateUserSystemRoles([FromBody] UpdateSystemRolesDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _dbContext.Users.Include(u => u.Claims).FirstOrDefault(u => u.Id == dto.UserId);

            if (user is null)
                return NotFound();

            var claim = user.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.SystemRole.GetClaimType());
            if (claim is null)
                user.Claims.Add(new UserClaim
                {
                    ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                    ClaimValue = Convert.ToInt32(dto.Roles).ToString()
                });
            else
            {
                claim.ClaimValue = Convert.ToInt32(dto.Roles).ToString();
            }

            _dbContext.SaveChanges();

            return Ok(dto);
        }

        [HttpPatch("generator-modifications")]
        public IActionResult UpdateUserGeneratorModifications([FromBody] UpdateGeneratorModificationsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = _dbContext.Users.FirstOrDefault(u => u.Id == dto.UserId);

            if (user is null)
                return NotFound();

            var claim = user.Claims.FirstOrDefault(c => c.ClaimType == ClaimTypes.GeneratorModifications.GetClaimType());
            if (claim is null)
                user.Claims.Add(new UserClaim
                {
                    ClaimType = ClaimTypes.GeneratorModifications.GetClaimType(),
                    ClaimValue = Convert.ToInt32(dto.Modifications).ToString()
                });
            else
            {
                claim.ClaimValue = Convert.ToInt32(dto.Modifications).ToString();
            }

            _dbContext.SaveChanges();

            return Ok(dto);
        }

        [HttpDelete()]
        public IActionResult DeleteUser([FromQuery] int userId)
        {
            lock (_lock)
            {
                User? user = _dbContext.Users.FirstOrDefault(t => t.Id == userId);
                if (user is null)
                {
                    return NotFound();
                }

                lock (_lock)
                {
                    _dbContext.Users.Remove(user);
                    _dbContext.SaveChanges();
                }

                return Ok();
            }
        }
    }
}
