using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Exceptions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;
using System.Security.Claims;

namespace ReviewVisualizer.WebApi.Services
{
    public class AuthService
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

            _dbContext.Users.Add(user);
            var recordsChanged = await _dbContext.SaveChangesAsync();

            return recordsChanged > 0;
        }

        public async Task<ClaimsPrincipal> LoginAsync(LoginRequest request)
        {
            var user = await _dbContext.Users
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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
    }
}
