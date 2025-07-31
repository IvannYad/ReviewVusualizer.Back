using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Exceptions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;

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

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
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
            await _dbContext.SaveChangesAsync();

            return new RegisterResponse(true, request.Username);
        }

        private bool RegisterRequestIsValid()
        {

        }
    }
}
