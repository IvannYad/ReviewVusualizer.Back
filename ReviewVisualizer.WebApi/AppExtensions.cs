using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.AuthLibrary.Enums;
using ReviewVisualizer.AuthLibrary.Extensions;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.WebApi.Processor;
using ReviewVisualizer.WebApi.RatingCalculationEngine;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace ReviewVisualizer.WebApi
{
    public static class AppExtensions
    {
        public static void StartRatingCalculationEngine(this WebApplication app)
        {
            var ratingCalculationEngine = app.Services.GetService<IRatingCalculatingEngine>();

            ratingCalculationEngine?.Start();
        }

        public static void StartProcessorHost(this WebApplication app)
        {
            var processotHost = app.Services.GetService<IProcessorHost>();

            processotHost?.Init();
            processotHost?.Start();
        }

        public static void AddAdminUser(this WebApplication app)
        {
            var configuration = app.Configuration;
            string adminUserName = configuration["Admin:UserName"]!;
            string adminPassword = configuration["Admin:Password"]!;

            if (string.IsNullOrEmpty(adminPassword) || string.IsNullOrEmpty(adminUserName))
                return;

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();

            var admin = dbContext.Users.FirstOrDefault(u => u.UserName == adminUserName);
            if (admin is null)
            {
                admin = new User
                {
                    UserName = adminUserName,
                    PasswordHash = passwordService.HashPassword(adminPassword),
                    Claims = new[]
                    {
                        new UserClaim
                        {
                            ClaimType = ClaimTypes.SystemRole.GetClaimType(),
                            ClaimValue = Convert.ToInt32(SystemRoles.Owner).ToString(),
                        }
                    }
                };

                dbContext.Users.Add(admin);
                dbContext.SaveChanges();
            }
        }
    }
}