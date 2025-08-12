using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IGeneratorHost
    {
        void GenerateFireAndForget(int reviewerId);
        void GenerateDelayed(int reviewerId, TimeSpan delay);
        void GenerateRecurring(int reviewerId, string cron);
        void GenerateReview(GenerateReviewRequest request);
        bool DeleteReviewer(int reviewerId);
        bool CreateReviewer(Reviewer reviewer);
    }
}