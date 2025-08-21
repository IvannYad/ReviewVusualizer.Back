using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.WebApi.Processor
{
    public class ProcessorHost : IProcessorHost
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ProcessorHost> _logger;
        private readonly ILogger<Analyst> _analystLogger;
        private readonly List<Analyst> _reviewers;
        private readonly Dictionary<Analyst, Thread?> _analystCollection;
        private bool _isInitialized = false;

        public ProcessorHost([FromServices] ApplicationDbContext dbContext,
            [FromServices] ILogger<ProcessorHost> logger,
            [FromServices] ILogger<Analyst> analystLogger)
        {

        }

        public void Init()
        {
            //if (_isInitialized) return;

            //_reviewers.ForEach(a => _analystCollection.Add(a, new Thread(() => a
            //    .ProcessReview(ApplicationDbContext.CreateNew(_dbContext), _queue, _analystLogger))));
            //foreach (var an in _analystCollection.Keys)
            //{
            //    an.ThreadCompleted += OnWorkerStopped;
            //}

            //_isInitialized = true;
        }

        public void Start()
        {
            //if (!_isInitialized) return;

            //_logger.LogInformation($"[ProcessorHost] Processor Host started");
            //foreach (var t in _analystCollection)
            //{
            //    if (!_reviewers.FirstOrDefault(r => r.Id == t.Key.Id)?.IsStopped ?? false && t.Value is not null)
            //    {
            //        _logger.LogInformation($"[ProcessorHost] Analyst {t.Key.Name} is started");
            //        t.Value?.Start();
            //    }
            //    else
            //    {
            //        _analystCollection[t.Key] = null;
            //    }
            //}
        }

        public bool CreateAnalyst(Analyst analyst)
        {
            if (!_isInitialized) return false;
            if (_analystCollection.ContainsKey(analyst)) return false;

            analyst.IsStopped = true;
            analyst.ThreadCompleted += OnWorkerStopped;
            _analystCollection.Add(analyst, null);
            _logger.LogInformation($"[ProcessorHost] Analyst {analyst.Name} is created in stopped state");
            return true;
        }

        public bool DeleteAnalyst(Analyst analyst)
        {
            if (!_isInitialized) return true;
            if (!_analystCollection.ContainsKey(analyst)) return true;

            try
            {
                _analystCollection[analyst]?.Interrupt();
                _analystCollection[analyst]?.Join();
                _analystCollection[analyst] = null;
                _analystCollection.Remove(analyst);
                _logger.LogInformation($"[ProcessorHost] Analyst {analyst.Name} is deleted");
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool StopAnalyst(int id)
        {
            if (!_isInitialized) return false;

            var analyst = _analystCollection.Keys.FirstOrDefault(r => r.Id == id);
            if (analyst is null) return false;

            _logger.LogInformation($"[ProcessorHost] Stopping analyst {analyst.Name}");
            analyst.IsStopped = true;
            return true;
        }

        public bool StartAnalyst(int id)
        {
            if (!_isInitialized) return false;

            var analyst = _analystCollection.Keys.FirstOrDefault(a => a.Id == id);
            if (analyst is null || analyst.IsStopped == false) return false;

            _logger.LogInformation($"[GeneratorHost] Starting reviewer {analyst.Name}");

            //analyst.IsStopped = false;
            //_analystCollection[analyst] = new Thread(() => analyst.ProcessReview(ApplicationDbContext.CreateNew(_dbContext), _queue, _analystLogger));
            //_analystCollection[analyst]?.Start();
            return true;
        }

        public void OnWorkerStopped(object sender, EventArgs e)
        {
            if (!_isInitialized) return;

            Analyst? analyst = sender as Analyst;
            if (analyst is null) return;

            _logger.LogInformation($"[ProcessorHost] Analyst {analyst.Name} is stopped");

            _analystCollection[analyst] = null;
        }
    }
}
