using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using System.Diagnostics.CodeAnalysis;

namespace ReviewVisualizer.WebApi.RatingCalculationEngine
{
    public class RatingCalculatingEngine : IRatingCalculatingEngine
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RatingCalculatingEngine> _logger;
        private Thread _teachersCalculator;
        private Thread _departmentsCalculator;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public RatingCalculatingEngine([FromServices] ApplicationDbContext dbContext, [FromServices] ILogger<RatingCalculatingEngine> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            _teachersCalculator = new Thread(() => UpdateTeachersRatings(ApplicationDbContext.CreateNew(_dbContext), _cancellationTokenSource.Token));
            _departmentsCalculator = new Thread(() => UpdateDepartmentRatings(ApplicationDbContext.CreateNew(_dbContext), _cancellationTokenSource.Token));
            _teachersCalculator.Start();
            _departmentsCalculator.Start();

            _logger.LogInformation($"[RatingCalculatingEngine] Engine started.");
        }
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _logger.LogInformation($"[RatingCalculatingEngine] Engine stopped.");
        }

        private void UpdateTeachersRatings(ApplicationDbContext dbContext, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var teachers = dbContext.Teachers.ToList();
                foreach (var teacher in teachers)
                {
                    var reviews = dbContext.Reviews.Where(r => r.TeacherId == teacher.Id).ToList();
                    double? avgRating = reviews.Count > 1 ? reviews.Average(r => r.Overall) : null;
                    teacher.Rating = avgRating is not null ? (double)Math.Round((decimal)avgRating, 2) : null;
                    dbContext.SaveChanges();
                    _logger.LogInformation($"[RatingCalculatingEngine] Rating for T:({teacher.FirstName} {teacher.LastName}) is UPDATED. New rating: {teacher.Rating}");
                }

                Thread.Sleep(5_000);
            }
        }

        private void UpdateDepartmentRatings(ApplicationDbContext dbContext, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var departments = dbContext.Departments.ToList();
                foreach (var department in departments)
                {
                    var avgRating = dbContext.Database.SqlQuery<double?>(
                        @$"
                            SELECT AVG(r.Overall) as Value
                            FROM Reviews r
                            INNER JOIN Teachers t ON t.Id = r.TeacherId
                            RIGHT JOIN Departments d ON d.Id = t.DepartmentId
                            GROUP BY d.Id
                            HAVING d.Id = {department.Id}
                        ").First();

                    department.Rating = avgRating is not null ? (double)Math.Round((decimal)avgRating, 2) : null;
                    dbContext.SaveChanges();
                    _logger.LogInformation($"[RatingCalculatingEngine] Rating for D:({department}) is UPDATED. New rating: {department.Rating}");
                }

                Thread.Sleep(5_000);
            }
        }
    }
}
