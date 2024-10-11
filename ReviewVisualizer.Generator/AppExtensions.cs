using ReviewVisualizer.Generator.Generator;

namespace ReviewVisualizer.Generator
{
    public static class AppExtensions
    {
        public static void StartGenerator(this WebApplication app)
        {
            IGeneratorHost host = app.Services.GetService<IGeneratorHost>();
            host?.Init();
            host?.Start();
        }
    }
}
