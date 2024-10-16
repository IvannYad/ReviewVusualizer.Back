using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using ReviewVisualizer.Data.Dto;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Models
{
    public class Reviewer: IEquatable<Reviewer>
    {
        public event EventHandler ThreadCompleted;

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(100, 100_000)]
        public int ReviewGenerationFrequensyMiliseconds { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int TeachingQualityMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int TeachingQualityMaxGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int StudentsSupportMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int StudentsSupportMaxGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int CommunicationMinGrage { get; set; }

        [Required]
        [Range(1.0, 100.0)]
        public int CommunicationMaxGrage { get; set; }

        public bool IsStopped { get; set; }

        [ValidateNever]
        public virtual ICollection<Teacher> Teachers { get; set; }

        public void GenerateReview(IQueueController queue, ILogger<Reviewer> logger)
        {
            if (Teachers is null || Teachers.Count() < 1)
            {
                return;
            }

            Random r = new Random();
            try
            {
                while (!IsStopped)
                {
                    var randomTeacher = Teachers.Count() > 0 ? Teachers.ElementAt(r.Next(Teachers.Count())) : null;

                    if (randomTeacher is null)
                    {
                        IsStopped = true;
                        break;
                    }

                    var review = new ReviewCreateDTO();
                    review.ReviewTime = DateTime.Now;
                    review.TeachingQuality = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                    review.StudentsSupport = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                    review.Communication = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                    review.Overall = (review.TeachingQuality + review.StudentsSupport + review.Communication) / 3;

                    review.TeacherId = randomTeacher.Id;

                    if (IsStopped) break;

                    queue.AddReview(review);
                    logger.LogInformation($"[Reviewer] Review for {randomTeacher.FirstName} {randomTeacher.LastName} is added [ Reviewer: {Name} ]");

                    Thread.Sleep(TimeSpan.FromMilliseconds(ReviewGenerationFrequensyMiliseconds));
                }
            }
            catch (ThreadInterruptedException)
            {
                if (!IsStopped) logger.LogInformation($"[Reviewer] Reviewer {Name} is interrupted");
                IsStopped = true;
                return;
            }
            
            ThreadCompleted.Invoke(this, EventArgs.Empty);
        }

        public bool Equals(Reviewer? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.Id == other.Id;
        }
    }
}
