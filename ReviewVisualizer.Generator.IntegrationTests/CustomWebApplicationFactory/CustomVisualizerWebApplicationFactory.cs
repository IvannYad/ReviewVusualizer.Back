using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReviewVisualizer.AuthLibrary;

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
        }
    }
}