using Autofac;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReviewVisualizer.Data.Models
{
    public class Reviewer: IEquatable<Reviewer>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

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

        [Required]
        public GeneratorType Type { get; set; }

        [ValidateNever]
        public virtual ICollection<Teacher> Teachers { get; set; }

        public void GenerateReview(ILogger<Reviewer>? logger, IMapper? mapper, ApplicationDbContext dbContext)
        {
            if (mapper is null || logger is null)
                throw new ArgumentNullException();

            logger.LogInformation($"Starting adding review from reviewer: {Name}");

            if (Teachers is null || Teachers.Count() < 1)
            {
                return;
            }

            Random r = new Random();
            var review = new ReviewCreateDTO();
            review.ReviewTime = DateTime.Now;
            review.TeachingQuality = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
            review.StudentsSupport = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
            review.Communication = r.Next(TeachingQualityMinGrage, TeachingQualityMaxGrage);
            review.Overall = (review.TeachingQuality + review.StudentsSupport + review.Communication) / 3;

            var randomTeacher = Teachers.ElementAt(r.Next(Teachers.Count()));
            review.TeacherId = randomTeacher.Id;

            try
            {
                // Let's imitate some heavy work that takes some time.
                Thread.Sleep(r.Next(500, 5000));

                var reviewEntity = mapper.Map<Review>(review);
                dbContext.Reviews.Add(reviewEntity);
                dbContext.SaveChanges();

                logger.LogInformation($"[Reviewer] Review for {randomTeacher.FirstName} {randomTeacher.LastName} is added [ Reviewer: {Name} ]");
            }
            catch
            {
                logger.LogError($"Error while adding review from {Name} for teacher {randomTeacher.FirstName} {randomTeacher.LastName}");
            }
        }

        public bool Equals(Reviewer? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.Id == other.Id;
        }
    }
}
