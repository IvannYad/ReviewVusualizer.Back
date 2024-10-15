using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.Data.Models
{
    public class Analyst : IEquatable<Analyst>
    {
        public event EventHandler ThreadCompleted;

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(100, 100_000)]
        public int ProcessingDurationMiliseconds { get; set; }

        public bool IsStopped { get; set; }

        public bool Equals(Analyst? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.Id == other.Id;
        }

        public void ProcessReview(ApplicationDbContext dbContext,
            IQueueController queue, 
            ILogger<Analyst> logger)
        {
            while (!IsStopped)
            {
                var review = queue.GetReview();
                if (review is not null)
                {
                    Thread.Sleep(ProcessingDurationMiliseconds);
                    dbContext.Reviews.Add(review);
                    dbContext.SaveChanges();
                    logger.LogInformation($"[Reviewer] Review is processed [ Analyst: {Name} ]");
                }
                else
                {
                    logger.LogInformation($"[Reviewer] There is no review in queue. Having a rest for 5 sec. [ Analyst: {Name} ]");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
