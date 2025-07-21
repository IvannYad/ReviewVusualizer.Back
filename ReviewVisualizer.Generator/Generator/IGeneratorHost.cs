using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IGeneratorHost
    {
        void GenerateFireAndForget(int reviewerId);
        void GenerateDelayed(int reviewerId, TimeSpan delay);
        void GenerateRecurring(int reviewerId, string cron);
        bool DeleteReviewer(int reviewerId);
        bool CreateReviewer(Reviewer reviewer);
    }
}