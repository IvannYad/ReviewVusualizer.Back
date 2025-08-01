using ReviewVisualizer.Data.Dto;
using System.Security.Claims;

namespace ReviewVisualizer.WebApi.Services
{
    public interface IAuthService
    {
        Task<ClaimsPrincipal> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
    }
}