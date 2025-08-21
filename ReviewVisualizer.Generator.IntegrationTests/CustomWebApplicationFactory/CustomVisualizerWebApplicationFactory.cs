using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReviewVisualizer.Data;

namespace ReviewVisualizer.Generator.IntegrationTests.CustomWebApplicationFactory
{
    public class CustomVisualizerWebApplicationFactory
        : WebApplicationFactory<VisualizerProject.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                var env = context.HostingEnvironment;

                configBuilder.Sources.Clear();

                var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.IntegrationTests.json");
                configBuilder
                    .AddJsonFile(path, optional: false)
                    .AddEnvironmentVariables();
            });

            //builder.ConfigureServices((context, services) =>
            //{
            //    var configuration = context.Configuration;

            //    // Replace real DB with in-memory DB for tests
            //    var descriptor = services.SingleOrDefault(
            //        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            //    if (descriptor != null)
            //        services.Remove(descriptor);

            //    services.AddDbContext<ApplicationDbContext>(options =>
            //    {
            //        options.UseInMemoryDatabase(configuration["TestDbName"]!);
            //    });

            //    services.ConfigureApplicationCookie(options =>
            //    {
            //        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            //        options.Cookie.SameSite = SameSiteMode.Lax;
            //    });
            //});
        }
    }
}