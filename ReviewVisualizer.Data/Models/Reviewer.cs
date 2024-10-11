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
        public int ReviewGenerationFrequensyMiliseconds { get; set; }

        [Required]
        public int TeachingQualityMinGrage { get; set; }

        [Required]
        public int TeachingQualityMaxGrage { get; set; }

        [Required]
        public int StudentsSupportMinGrage { get; set; }

        [Required]
        public int StudentsSupportMaxGrage { get; set; }

        [Required]
        public int CommunicationMinGrage { get; set; }

        [Required]
        public int CommunicationMaxGrage { get; set; }

        public bool IsStopped { get; set; }

        [ValidateNever]
        public virtual ICollection<Teacher> Teachers { get; set; }

        public void GenerateReview(ApplicationDbContext dbContext, ILogger<Reviewer> logger)
        {
            if (Teachers is null || Teachers.Count() < 1)
            {
                return;
            }

            Random r = new Random();
            while (!IsStopped)
            {
                var review = new Review();
                review.ReviewTime = DateTime.Now;
                review.TeachingQuality = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                review.StudentsSupport = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                review.Communication = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
                review.Overall = (review.TeachingQuality + review.StudentsSupport + review.Communication) / 3;

                var randomTeacher = Teachers.ElementAt(r.Next(Teachers.Count()));

                review.Teacher = randomTeacher;
                review.TeacherId = randomTeacher.Id;

                if (IsStopped) break;
                dbContext.Reviews.Add(review);
                dbContext.SaveChanges();
                logger.LogInformation($"Review for teacher {randomTeacher.FirstName + " " + randomTeacher.LastName} is added. Reviewer - {Name}");

                Thread.Sleep(TimeSpan.FromMilliseconds(ReviewGenerationFrequensyMiliseconds));
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
