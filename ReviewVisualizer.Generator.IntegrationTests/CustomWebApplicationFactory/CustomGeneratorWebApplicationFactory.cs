using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReviewVisualizer.AuthLibrary;

namespace ReviewVisualizer.Generator.IntegrationTests.CustomWebApplicationFactory
{
    public class CustomGeneratorWebApplicationFactory
        : WebApplicationFactory<GeneratorProject.Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Add Autofac for test host
            builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            return base.CreateHost(builder);
        }

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

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);
            client.BaseAddress = new Uri("http://localhost:5001/");
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            builder.UseUrls("http://localhost:5001");

            var server = base.CreateServer(builder);
            return server;
        }
    }
}