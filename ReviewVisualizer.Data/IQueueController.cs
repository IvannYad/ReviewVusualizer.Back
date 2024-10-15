using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Data
{
    public interface IQueueController
    {
        void AddReview(ReviewCreateDTO review);
        public Review? GetReview();
        public int GetQueueSize();
    }
}