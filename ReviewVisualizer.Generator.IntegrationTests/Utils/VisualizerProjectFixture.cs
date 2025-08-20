using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Generator.IntegrationTests.CustomWebApplicationFactory;
using ReviewVisualizer.TestUtils;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ReviewVisualizer.Generator.IntegrationTests.Utils
{
    public class VisualizerProjectFixture : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public HttpHandlersFactory HandlersFactory { get; private set; }
        public CustomVisualizerWebApplicationFactory VisualizerFactory { get; }
        public CustomGeneratorWebApplicationFactory GeneratorFactory { get; }

        public VisualizerProjectFixture()
        {
            VisualizerFactory = new CustomVisualizerWebApplicationFactory();
            GeneratorFactory = new CustomGeneratorWebApplicationFactory();

            // Create dbContext.
            using var scope = VisualizerFactory.Services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed test data.
            string passwordSecret = configuration["PasswordSecret"]!;
            var usersTypesAssociations = new Dictionary<TestUser, User>();
            TestDataSeeding.SeedData(_context, new PasswordService(passwordSecret), usersTypesAssociations);

            // Get clients for every user type.
            RetrieveCookiesAndHttpHandlers(usersTypesAssociations).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            VisualizerFactory.Dispose();
        }

        private async Task RetrieveCookiesAndHttpHandlers(Dictionary<TestUser, User> usersTypesAssociations)
        {
            var authClient = VisualizerFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false // Important for login redirects.
            });

            var usersCookies = new Dictionary<TestUser, CookieContainer>();

            foreach (TestUser userType in usersTypesAssociations.Keys)
            {
                var user = usersTypesAssociations[userType];
                if (!VerifyUserInTheDatabase(user.Id))
                    continue;

                var cookieContainer = new CookieContainer();
                
                if (userType is TestUser.NotAuthorized)
                {
                    usersCookies.Add(userType, new CookieContainer());
                    continue;
                }

                var loginRequest = new LoginRequest()
                {
                    Username = user.UserName,
                    Password = TestDataSeeding.TestUserPassword,
                };

                var loginResponse = await authClient.PostAsync(
                    "/auth/login",
                    new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json")
                );

                if (loginResponse is null || loginResponse.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"Cannot obtain client for {user.UserName}");

                var cookieHeader = loginResponse.Headers.GetValues("Set-Cookie").First();
                
                // Since BaseAddress for Visualizer and Generator are the same not need to set cookie for both of them.
                cookieContainer.SetCookies(GeneratorFactory.Server.BaseAddress,
                    cookieHeader);

                usersCookies.Add(userType, cookieContainer);
            }

            HandlersFactory = new HttpHandlersFactory(usersCookies);
        }

        private bool VerifyUserInTheDatabase(int userId)
        {
            return _context.Users.Any(user => user.Id == userId);
        }
    }
}
