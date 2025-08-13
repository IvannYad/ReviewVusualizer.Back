using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public interface IGeneratorHost
    {
        void GenerateReview(GenerateReviewRequest request);
        bool DeleteReviewer(int reviewerId);
        bool CreateReviewer(Reviewer reviewer);
    }
}