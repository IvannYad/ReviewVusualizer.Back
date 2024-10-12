using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.WebApi.RatingCalculationEngine
{
    public class RatingCalculatingEngine : IRatingCalculatingEngine
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RatingCalculatingEngine> _logger;
        private Thread _worker;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public RatingCalculatingEngine([FromServices] ApplicationDbContext dbContext, [FromServices] ILogger<RatingCalculatingEngine> logger)
        {
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _worker = new Thread(() => UpdateRatings(_cancellationTokenSource.Token));
            _worker.Start();
            _logger.LogInformation($"[RatingCalculatingEngine] Engine started.");
        }
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInformation($"[RatingCalculatingEngine] Engine stopped.");
        }

        private void UpdateRatings(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var teachers = _dbContext.Teachers.ToList();
                foreach (var teacher in teachers)
                {
                    var avgRating = _dbContext.Reviews.Where(r => r.TeacherId == teacher.Id).Average(r => r.Overall);
                    teacher.Rating = Math.Round(avgRating, 2);
                    _dbContext.SaveChanges();
                    _logger.LogInformation($"[RatingCalculatingEngine] Rating for {teacher.FirstName} {teacher.LastName} is UPDATED. New rating: {teacher.Rating}");
                }

                Thread.Sleep(5_000);
            }
        }
    }
}
