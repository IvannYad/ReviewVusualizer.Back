using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using System.Transactions;

namespace ReviewVisualizer.Generator.Generator
{
    public class GeneratorHost : IGeneratorHost
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<GeneratorHost> _logger;
        private readonly ILogger<Reviewer> _reviewerLogger;
        private readonly List<Reviewer> _reviewers;

        public GeneratorHost([FromServices]ApplicationDbContext dbContext,
            [FromServices]ILogger<GeneratorHost> logger,
            [FromServices]ILogger<Reviewer> reviewerLogger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _reviewerLogger = reviewerLogger;
            _reviewers = _dbContext.Reviewers.Include(r => r.Teachers).ToList();
        }

        public bool CreateReviewer(Reviewer reviewer)
        {
            _reviewers.Add(reviewer);
            return true;
        }

        public bool DeleteReviewer(int reviewerId)
        {
            throw new NotImplementedException();
        }

        public void GenerateDelayed(int reviewerId, TimeSpan delay)
        {
            _logger.LogInformation($"Generating \"Delayed\" job for reviewer with ID: {reviewerId}");
            
            var reviewer = _reviewers.FirstOrDefault(r => r.Id == reviewerId);
            ArgumentNullException.ThrowIfNull(reviewer, nameof(reviewer));

            // Schedule job.
            _logger.LogInformation($"\"Delayed\" review generation job is scheduled for reviewer {reviewer.Name}");
        }

        public void GenerateFireAndForget(int reviewerId)
        {
            _logger.LogInformation($"Generating \"Fire And Forget\" job for reviewer with ID: {reviewerId}");

            var reviewer = _reviewers.FirstOrDefault(r => r.Id == reviewerId);
            ArgumentNullException.ThrowIfNull(reviewer, nameof(reviewer));

            // Schedule job.
            _logger.LogInformation($"\"Fire And Forget\" review generation job is scheduled for reviewer {reviewer.Name}");
        }

        public void GenerateRecurring(int reviewerId, TimeSpan interval)
        {
            _logger.LogInformation($"Generating \"Recurring\" job for reviewer with ID: {reviewerId}");

            var reviewer = _reviewers.FirstOrDefault(r => r.Id == reviewerId);
            ArgumentNullException.ThrowIfNull(reviewer, nameof(reviewer));

            // Schedule job.
            _logger.LogInformation($"\"Recurring\" review generation job is scheduled for reviewer {reviewer.Name}");
        }
    }
}
