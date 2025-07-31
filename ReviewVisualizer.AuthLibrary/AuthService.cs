using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;

namespace ReviewVisualizer.WebApi.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists.
            
        }

        private bool RegisterRequestIsValid()
        {

        }
    }
}
