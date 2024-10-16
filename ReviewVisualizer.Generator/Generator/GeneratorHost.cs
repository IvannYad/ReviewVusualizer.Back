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
        private readonly IQueueController _queue;
        private readonly List<Reviewer> _reviewers;
        private readonly Dictionary<Reviewer, Thread?> _reviewersCollection;
        private bool _isInitialized = false;

        public GeneratorHost([FromServices]ApplicationDbContext dbContext,
            [FromServices]ILogger<GeneratorHost> logger,
            [FromServices]ILogger<Reviewer> reviewerLogger,
            [FromServices]IQueueController queue)
        {
            _dbContext = dbContext;
            _logger = logger;
            _reviewerLogger = reviewerLogger;
            _queue = queue;
            _reviewers = _dbContext.Reviewers.Include(r => r.Teachers).ToList();
            _reviewersCollection = new Dictionary<Reviewer, Thread?>();
        }

        public void Init()
        {
            if (_isInitialized) return;
            
            _reviewers.ForEach(r => _reviewersCollection.Add(r, new Thread(() => r.GenerateReview(_queue, _reviewerLogger))));
            foreach(var rev in _reviewersCollection.Keys)
            {
                rev.ThreadCompleted += OnWorkerStopped;
            }

            _isInitialized = true;
        }

        public void Start()
        {
            if (!_isInitialized) return;

            _logger.LogInformation($"[GeneratorHost] Generator Host started");
            foreach (var t in _reviewersCollection)
            {
                if (!_reviewers.FirstOrDefault(r => r.Id == t.Key.Id)?.IsStopped ?? false && t.Value is not null)
                {
                    _logger.LogInformation($"[GeneratorHost] Reviewer {t.Key.Name} is started");
                    t.Value?.Start();
                }
                else
                {
                    _reviewersCollection[t.Key] = null;
                }
            }
        }

        public bool CreateReviewer(Reviewer reviewer)
        {
            if (!_isInitialized) return false;
            if (_reviewersCollection.ContainsKey(reviewer)) return false;

            reviewer.IsStopped = true;
            reviewer.ThreadCompleted += OnWorkerStopped;
            _reviewersCollection.Add(reviewer, null);
            _logger.LogInformation($"[GeneratorHost] Reviewer {reviewer.Name} is created in stopped state");
            return true;
        }

        public bool DeleteReviewer(Reviewer reviewer)
        {
            if (!_isInitialized) return true;
            if (!_reviewersCollection.ContainsKey(reviewer)) return true;

            try
            {
                _reviewersCollection[reviewer]?.Interrupt();
                _reviewersCollection[reviewer]?.Join();
                _reviewersCollection[reviewer] = null;
                _reviewersCollection.Remove(reviewer);
                _logger.LogInformation($"[GeneratorHost] Reviewer {reviewer.Name} is deleted");
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool StopReviewer(int id)
        {
            if (!_isInitialized) return false;
            
            var reviewer = _reviewersCollection.Keys.FirstOrDefault(r => r.Id == id);
            if (reviewer is null) return false;

            _logger.LogInformation($"[GeneratorHost] Stopping reviewer {reviewer.Name}");
            reviewer.IsStopped = true;
            return true;
        }

        public bool StartReviewer(int id)
        {
            if (!_isInitialized) return false;

            var reviewer = _reviewersCollection.Keys.FirstOrDefault(r => r.Id == id);
            if (reviewer is null || reviewer.IsStopped == false || reviewer.Teachers?.Count() == 0) return false;

            _logger.LogInformation($"[GeneratorHost] Starting reviewer {reviewer.Name}");

            reviewer.IsStopped = false;
            _reviewersCollection[reviewer] ??= new Thread(() => reviewer.GenerateReview(_queue, _reviewerLogger));
            _reviewersCollection[reviewer]?.Start();
            return true;
        }

        public void OnWorkerStopped(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            Reviewer? reviewer = sender as Reviewer;
            if (reviewer is null) return;

            _logger.LogInformation($"[GeneratorHost] Reviewer {reviewer.Name} is stopped");

            _reviewersCollection[reviewer] = null;
        }
    }
}
