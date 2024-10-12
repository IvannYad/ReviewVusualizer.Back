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
    }
}
