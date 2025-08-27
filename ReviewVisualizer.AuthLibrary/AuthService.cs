using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Exceptions;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using MyClaimTypes = ReviewVisualizer.AuthLibrary.Enums.ClaimTypes;

namespace ReviewVisualizer.WebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly PasswordService _passwordService;

        public AuthService(ApplicationDbContext dbContext, PasswordService passwordService)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists.
            if (await _dbContext.Users.AnyAsync(u => u.UserName == request.Username))
                throw new UserAlreadyExistsException(request.Username);

            var user = new User()
            {
                UserName = request.Username,
                PasswordHash = _passwordService.HashPassword(request.Password)
            };

            user.SetSystemRoles(SystemRoles.Visitor);
            user.SetGeneratorModifications(GeneratorModifications.View);

            _dbContext.Users.Add(user);
            var recordsChanged = await _dbContext.SaveChangesAsync();

            return recordsChanged > 0;
        }

        public async Task<ClaimsPrincipal> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _dbContext.Users
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

                if (user == null || !_passwordService.VerifyPassword(user.PasswordHash, request.Password))
                    throw new UserUnauthenticatedException(request.Username, request.Password);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                };

                claims.AddRange(user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                return principal;
            }
            catch (Exception ex)
            {
                throw new UserUnauthenticatedException(request.Username, request.Password, ex);
            }
        }
    }
}