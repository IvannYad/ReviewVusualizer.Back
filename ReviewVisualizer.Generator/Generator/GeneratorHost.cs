using Autofac;
using AutoMapper;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Generator.Generator
{
    public class GeneratorHost : IGeneratorHost
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<GeneratorHost> _logger;
        protected readonly List<Reviewer> _reviewers;
        private readonly ILifetimeScope _container;
        private readonly IMapper _mapper;
        private static object _locker = new object();

        public GeneratorHost(ILifetimeScope container)
        {
            lock (_locker)
            {
                _container = container;
                var scope = _container.BeginLifetimeScope();
                
                _dbContext = scope.Resolve<ApplicationDbContext>();
                _logger = scope.Resolve<ILogger<GeneratorHost>>();
                _reviewers = _dbContext.Reviewers.Include(r => r.Teachers).ToList();
                _mapper = scope.Resolve<IMapper>();
            }
        }

        public bool CreateReviewer(Reviewer reviewer)
        {
            _reviewers.Add(reviewer);
            return true;
        }

        public bool DeleteReviewer(int reviewerId)
        {
            var reviewer = _reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is not null)
            {
                if(reviewer.Type == Data.Enums.GeneratorType.RECURRING)
                    RecurringJob.RemoveIfExists(reviewerId.ToString());

                return _reviewers.Remove(reviewer);
            }

            return true;
        }

        public void GenerateDelayed(int reviewerId, TimeSpan delay)
        {
            _logger.LogInformation($"Generating \"Delayed\" job for reviewer with ID: {reviewerId}");
            
            BackgroundJob.Schedule("delayed_queue", () => GenerateReview(reviewerId), delay);
            _logger.LogInformation($"\"Delayed\" review generation job is scheduled.");
        }

        public void GenerateFireAndForget(int reviewerId)
        {
            _logger.LogInformation($"Generating \"Fire And Forget\" job for reviewer with ID: {reviewerId}");

            BackgroundJob.Enqueue(() => GenerateReview(reviewerId));
            _logger.LogInformation($"\"Fire And Forget\" review generation job is scheduled");
        }

        public void GenerateRecurring(int reviewerId, string cron)
        {
            _logger.LogInformation($"Generating \"Recurring\" job for reviewer with ID: {reviewerId}");

            RecurringJob.RemoveIfExists(reviewerId.ToString());
            RecurringJob.AddOrUpdate(reviewerId.ToString(), () => GenerateReview(reviewerId), cron);

            _logger.LogInformation($"\"Recurring\" review generation job is scheduled with cron expression {cron}");
        }

        public void GenerateReview(int reviewerId)
        {
            lock (_locker)
            {
                var reviewer = _reviewers.FirstOrDefault(r => r.Id == reviewerId);
                if (reviewer is null)
                    return;

                using var scope = _container.BeginLifetimeScope();
                var reviewerLogger = scope.Resolve<ILogger<Reviewer>>();
                reviewer.GenerateReview(reviewerLogger, _mapper, _dbContext);
                _logger.LogInformation($"Review generation job is scheduled from reviewer {reviewer.Name}");
            }
        }

        public void GenerateReview(GenerateReviewRequest request)
        {
            switch (request.Type)
            {
                case GeneratorType.FIRE_AND_FORGET:
                    GenerateFireAndForget(request.ReviewerId);
                    break;
                case GeneratorType.DELAYED:
                    GenerateDelayed(request.ReviewerId, request.Delay!.Value);
                    break;
                case GeneratorType.RECURRING:
                    GenerateRecurring(request.ReviewerId, request.Cron!);
                    break;
                default:
                    throw new InvalidOperationException(nameof(request.Type));
            }
        }
    }
}
