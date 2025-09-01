using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using System.Diagnostics;
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
        private readonly Stopwatch _stopwatch;

        public RatingCalculatingEngine([FromServices] ApplicationDbContext dbContext, [FromServices] ILogger<RatingCalculatingEngine> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch = new Stopwatch();

            _logger.LogInformation("[RatingCalculatingEngine] Engine instance created successfully");
        }

        public void Start()
        {
            try
            {
                _logger.LogInformation("[RatingCalculatingEngine] Starting rating calculation engine...");

                _teachersCalculator = new Thread(() => UpdateTeachersRatings(ApplicationDbContext.CreateNew(_dbContext), _cancellationTokenSource.Token))
                {
                    Name = "TeachersRatingCalculator",
                    IsBackground = true
                };

                _departmentsCalculator = new Thread(() => UpdateDepartmentRatings(ApplicationDbContext.CreateNew(_dbContext), _cancellationTokenSource.Token))
                {
                    Name = "DepartmentsRatingCalculator",
                    IsBackground = true
                };

                _teachersCalculator.Start();
                _departmentsCalculator.Start();

                _logger.LogInformation("[RatingCalculatingEngine] Engine started successfully. Teachers calculator: {TeachersThreadId}, Departments calculator: {DepartmentsThreadId}",
                    _teachersCalculator.ManagedThreadId, _departmentsCalculator.ManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RatingCalculatingEngine] Failed to start rating calculation engine");
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                _logger.LogInformation("[RatingCalculatingEngine] Stopping rating calculation engine...");

                _cancellationTokenSource.Cancel();

                // Wait for threads to complete gracefully
                if (_teachersCalculator?.IsAlive == true)
                {
                    _logger.LogDebug("[RatingCalculatingEngine] Waiting for teachers calculator thread to complete...");
                    _teachersCalculator.Join(TimeSpan.FromSeconds(10));
                }

                if (_departmentsCalculator?.IsAlive == true)
                {
                    _logger.LogDebug("[RatingCalculatingEngine] Waiting for departments calculator thread to complete...");
                    _departmentsCalculator.Join(TimeSpan.FromSeconds(10));
                }

                _logger.LogInformation("[RatingCalculatingEngine] Engine stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RatingCalculatingEngine] Error occurred while stopping the engine");
            }
        }

        private void UpdateTeachersRatings(ApplicationDbContext dbContext, CancellationToken token)
        {
            _logger.LogInformation("[TeachersRatingCalculator] Starting teachers rating calculation thread");

            var iterationCount = 0;
            while (!token.IsCancellationRequested)
            {
                iterationCount++;
                _stopwatch.Restart();

                try
                {
                    _logger.LogDebug("[TeachersRatingCalculator] Starting iteration {IterationCount}", iterationCount);

                    var teachers = dbContext.Teachers.ToList();
                    _logger.LogDebug("[TeachersRatingCalculator] Retrieved {TeacherCount} teachers from database", teachers.Count);

                    var updatedCount = 0;
                    var unchangedCount = 0;

                    foreach (var teacher in teachers)
                    {
                        try
                        {
                            var reviews = dbContext.Reviews.Where(r => r.TeacherId == teacher.Id).ToList();
                            var previousRating = teacher.Rating;

                            double? avgRating = reviews.Count > 0 ? reviews.Average(r => r.Overall) : null;
                            double? newRating = avgRating is not null ? (double)Math.Round((decimal)avgRating, 2) : null;

                            if (teacher.Rating != newRating)
                            {
                                teacher.Rating = newRating;
                                updatedCount++;

                                _logger.LogInformation("[TeachersRatingCalculator] Teacher '{TeacherName}' (ID: {TeacherId}) rating updated: {PreviousRating} → {NewRating} (based on {ReviewCount} reviews)",
                                    $"{teacher.FirstName} {teacher.LastName}", teacher.Id, previousRating, newRating, reviews.Count);
                            }
                            else
                            {
                                unchangedCount++;
                                _logger.LogDebug("[TeachersRatingCalculator] Teacher '{TeacherName}' (ID: {TeacherId}) rating unchanged: {Rating} (based on {ReviewCount} reviews)",
                                    $"{teacher.FirstName} {teacher.LastName}", teacher.Id, teacher.Rating, reviews.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[TeachersRatingCalculator] Error processing teacher {TeacherId} ({TeacherName})",
                                teacher.Id, $"{teacher.FirstName} {teacher.LastName}");
                        }
                    }

                    dbContext.SaveChanges();
                    _stopwatch.Stop();

                    _logger.LogInformation("[TeachersRatingCalculator] Iteration {IterationCount} completed in {ElapsedMs}ms. Updated: {UpdatedCount}, Unchanged: {UnchangedCount}, Total: {TotalCount}",
                        iterationCount, _stopwatch.ElapsedMilliseconds, updatedCount, unchangedCount, teachers.Count);
                }
                catch (Exception ex)
                {
                    _stopwatch.Stop();
                    _logger.LogError(ex, "[TeachersRatingCalculator] Error in iteration {IterationCount} after {ElapsedMs}ms",
                        iterationCount, _stopwatch.ElapsedMilliseconds);
                }

                if (!token.IsCancellationRequested)
                {
                    _logger.LogDebug("[TeachersRatingCalculator] Waiting 5 seconds before next iteration...");
                    Thread.Sleep(5_000);
                }
            }

            _logger.LogInformation("[TeachersRatingCalculator] Teachers rating calculation thread stopped");
        }

        private void UpdateDepartmentRatings(ApplicationDbContext dbContext, CancellationToken token)
        {
            _logger.LogInformation("[DepartmentsRatingCalculator] Starting departments rating calculation thread");

            var iterationCount = 0;
            while (!token.IsCancellationRequested)
            {
                iterationCount++;
                _stopwatch.Restart();

                try
                {
                    _logger.LogDebug("[DepartmentsRatingCalculator] Starting iteration {IterationCount}", iterationCount);

                    var departments = dbContext.Departments.ToList();
                    _logger.LogDebug("[DepartmentsRatingCalculator] Retrieved {DepartmentCount} departments from database", departments.Count);

                    var updatedCount = 0;
                    var unchangedCount = 0;

                    foreach (var department in departments)
                    {
                        try
                        {
                            var previousRating = department.Rating;

                            var avgRating = dbContext.Database.SqlQuery<double?>(
                                @$"
                                    SELECT AVG(r.Overall) as Value
                                    FROM Reviews r
                                    INNER JOIN Teachers t ON t.Id = r.TeacherId
                                    RIGHT JOIN Departments d ON d.Id = t.DepartmentId
                                    GROUP BY d.Id
                                    HAVING d.Id = {department.Id}
                                ").First();

                            double? newRating = avgRating is not null ? (double)Math.Round((decimal)avgRating, 2) : null;

                            if (department.Rating != newRating)
                            {
                                department.Rating = newRating;
                                updatedCount++;

                                _logger.LogInformation("[DepartmentsRatingCalculator] Department '{DepartmentName}' (ID: {DepartmentId}) rating updated: {PreviousRating} → {NewRating}",
                                    department.Name, department.Id, previousRating, newRating);
                            }
                            else
                            {
                                unchangedCount++;
                                _logger.LogDebug("[DepartmentsRatingCalculator] Department '{DepartmentName}' (ID: {DepartmentId}) rating unchanged: {Rating}",
                                    department.Name, department.Id, department.Rating);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[DepartmentsRatingCalculator] Error processing department {DepartmentId} ({DepartmentName})",
                                department.Id, department.Name);
                        }
                    }

                    dbContext.SaveChanges();
                    _stopwatch.Stop();

                    _logger.LogInformation("[DepartmentsRatingCalculator] Iteration {IterationCount} completed in {ElapsedMs}ms. Updated: {UpdatedCount}, Unchanged: {UnchangedCount}, Total: {TotalCount}",
                        iterationCount, _stopwatch.ElapsedMilliseconds, updatedCount, unchangedCount, departments.Count);
                }
                catch (Exception ex)
                {
                    _stopwatch.Stop();
                    _logger.LogError(ex, "[DepartmentsRatingCalculator] Error in iteration {IterationCount} after {ElapsedMs}ms",
                        iterationCount, _stopwatch.ElapsedMilliseconds);
                }

                if (!token.IsCancellationRequested)
                {
                    _logger.LogDebug("[DepartmentsRatingCalculator] Waiting 5 seconds before next iteration...");
                    Thread.Sleep(5_000);
                }
            }

            _logger.LogInformation("[DepartmentsRatingCalculator] Departments rating calculation thread stopped");
        }
    }
}