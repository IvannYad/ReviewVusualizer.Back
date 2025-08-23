using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ReviewVisualizer.Data;

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
            client.BaseAddress = new Uri("https://localhost:5002/");
        }

        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            builder.UseUrls("https://localhost:5002");

            var server = base.CreateServer(builder);
            return server;
        }
    }
}