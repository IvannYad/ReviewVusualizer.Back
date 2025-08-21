using Hangfire;
using ReviewVisualizer.Generator.Generator;

namespace ReviewVisualizer.Generator.Utils
{
    public static class HangfireExtensions
    {
        public static void AddHangfireServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddHangfire(configuration => configuration
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(config.GetConnectionString("HangfireConnection")));
            services.AddHangfireServer(options =>
            {
                options.Queues = ["delayed_queue", "default"];
            });
        }
    }
}