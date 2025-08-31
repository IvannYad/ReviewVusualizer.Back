using Autofac;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Generator.Generator;

namespace ReviewVisualizer.Generator.Controllers
{
    [ApiController]
    [Route("reviewers")]
    [Authorize(Policy = Policies.RequireGeneratorAdmin)]
    public class ReviewerController : ControllerBase
    {
        private readonly ILogger<ReviewerController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IGeneratorHost _generatorHost;
        private readonly ILifetimeScope _container;
        private readonly IAuthorizationService _authorizationService;

        public ReviewerController(ILifetimeScope container, IAuthorizationService authorizationService)
        {
            _container = container;

            var scope = _container.BeginLifetimeScope();

            _dbContext = scope.Resolve<ApplicationDbContext>();
            _logger = scope.Resolve<ILogger<ReviewerController>>();
            _mapper = scope.Resolve<IMapper>();
            _generatorHost = scope.Resolve<IGeneratorHost>();
            _authorizationService = authorizationService;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var reviewers = _dbContext.Reviewers.AsNoTracking().Include(r => r.Teachers).ToList();
            return Ok(reviewers);
        }

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            _logger.LogInformation("Get endpoint called for reviewer ID: {Id}", id);

            var reviewer = _dbContext.Reviewers
                .AsNoTracking()
                .Include(r => r.Teachers)
                .FirstOrDefault(r => r.Id == id);

            if (reviewer is null)
            {
                _logger.LogWarning("Reviewer with ID {Id} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved reviewer {Name} with ID {Id}", reviewer.Name, id);
            return Ok(reviewer);
        }

        [HttpGet("type/{type:generatorType}")]
        public IActionResult GetByGeneratorType([FromRoute] GeneratorType type)
        {
            _logger.LogInformation("GetByGeneratorType endpoint called for type: {Type}", type);

            var reviewers = _dbContext.Reviewers
                .AsNoTracking()
                .Include(r => r.Teachers)
                .Where(r => r.Type == type);

            if (!reviewers.Any())
            {
                _logger.LogWarning("No reviewers found for type: {Type}", type);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved {Count} reviewers for type {Type}", reviewers.Count(), type);
            return Ok(reviewers);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateAsync([FromBody] ReviewerCreateDTO reviewerDTO)
        {
            _logger.LogInformation("CreateAsync endpoint called for reviewer type: {Type}", reviewerDTO.Type);

            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();  // Will prompt to select a debugger (Visual Studio, etc.)
            //}
            //Console.WriteLine("Debugger is attached: " + Debugger.IsAttached);

            if (!(await IsUserAuthorizedForModificationAsync(reviewerDTO.Type).ConfigureAwait(false)))
            {
                _logger.LogWarning("User not authorized to create reviewer of type: {Type}", reviewerDTO.Type);
                return Forbid();
            }

            var reviewer = _mapper.Map<Reviewer>(reviewerDTO);
            reviewer.Teachers = new List<Teacher>();

            _logger.LogInformation("Creating reviewer in generator for type: {Type}", reviewerDTO.Type);
            // Create new reviewer in generator.
            bool success = _generatorHost.CreateReviewer(reviewer);
            //Debugger.Log(1, "Generator", $"Reviewer {reviewerDTO.Type} is created with result: {success}");

            if (success)
            {
                _logger.LogInformation("Generator creation successful, adding to database");
                // Create new reviewer in database.
                _dbContext.Reviewers.Add(reviewer);
                _dbContext.SaveChanges();
                _logger.LogInformation("Reviewer {Name} successfully created in database with ID {Id}", reviewer.Name, reviewer.Id);
            }
            else
            {
                _logger.LogError("Failed to create reviewer in generator for type: {Type}", reviewerDTO.Type);
            }

            return Ok(success ? reviewer : null);
        }

        [HttpDelete()]
        public async Task<IActionResult> DeleteAsync([FromQuery] int reviewerId)
        {
            _logger.LogInformation("DeleteAsync endpoint called for reviewer ID: {Id}", reviewerId);

            //Debugger.Break();
            var reviewer = await _dbContext.Reviewers.FirstOrDefaultAsync(r => r.Id == reviewerId).ConfigureAwait(false);
            if (reviewer is null)
            {
                _logger.LogWarning("Reviewer with ID {Id} not found for deletion", reviewerId);
                return Ok();
            }

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
            {
                _logger.LogWarning("User not authorized to delete reviewer of type: {Type}", reviewer.Type);
                return Forbid();
            }

            _logger.LogInformation("Deleting reviewer {Name} with ID {Id} from generator", reviewer.Name, reviewer.Id);
            if (_generatorHost.DeleteReviewer(reviewer.Id))
            {
                _dbContext.Reviewers.Remove(reviewer);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Reviewer {Name} with ID {Id} successfully deleted", reviewer.Name, reviewer.Id);
                return Ok();
            }

            _logger.LogError("Failed to delete reviewer {Name} with ID {Id} from generator", reviewer.Name, reviewer.Id);
            return BadRequest($"Error occurred while deleting reviewer {reviewer.Name}");
        }

        [HttpPost("generate-review")]
        public async Task<IActionResult> Generate([FromBody] GenerateReviewRequest request)
        {
            _logger.LogInformation("Generate endpoint called for reviewer ID: {ReviewerId}, type: {Type}", request.ReviewerId, request.Type);

            try
            {
                if (!(await IsUserAuthorizedForModificationAsync(request.Type).ConfigureAwait(false)))
                {
                    _logger.LogWarning("User not authorized to generate review for type: {Type}", request.Type);
                    return Forbid();
                }

                _logger.LogInformation("Generating review for reviewer ID: {ReviewerId}", request.ReviewerId);
                return Ok("Papapapa");
                var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == request.ReviewerId);
                if (reviewer is null)
                {
                    _logger.LogWarning("Reviewer with ID {ReviewerId} not found for review generation", request.ReviewerId);
                    return NotFound();
                }

                _generatorHost.GenerateReview(request);
                _logger.LogInformation("Review generation initiated for reviewer {Name} with ID {Id}", reviewer.Name, request.ReviewerId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating review for reviewer ID: {ReviewerId}", request.ReviewerId);
                return BadRequest(ex);
            }
        }

        [HttpPost("add-teachers")]
        public async Task<IActionResult> AddTeachersAsync([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
            _logger.LogInformation("AddTeachersAsync endpoint called for reviewer ID: {ReviewerId} with {TeacherCount} teacher IDs", reviewerId, teacherIds.Length);

            var reviewer = _dbContext.Reviewers.Include(r => r.Teachers).FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null)
            {
                _logger.LogWarning("Reviewer with ID {ReviewerId} not found for adding teachers", reviewerId);
                return NotFound();
            }

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
            {
                _logger.LogWarning("User not authorized to add teachers to reviewer of type: {Type}", reviewer.Type);
                return Forbid();
            }

            if (reviewer.Teachers is null) reviewer.Teachers = new List<Teacher>();
            var newTeachersForReview = teacherIds.Where(id => !reviewer.Teachers.Any(teacher => teacher.Id == id));
            var chosenTeachers = _dbContext.Teachers.Where(t => newTeachersForReview.Contains(t.Id)).ToList();

            _logger.LogInformation("Adding {TeacherCount} teachers to reviewer {ReviewerName}", chosenTeachers.Count, reviewer.Name);
            chosenTeachers.ForEach(t => reviewer.Teachers.Add(t));
            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", chosenTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are added to reviewer {reviewer.Name}");
            return Ok(chosenTeachers);
        }

        [HttpPost("remove-teachers")]
        public async Task<IActionResult> RemoveTeachers([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
            _logger.LogInformation("RemoveTeachers endpoint called for reviewer ID: {ReviewerId} with {TeacherCount} teacher IDs", reviewerId, teacherIds.Length);

            var reviewer = _dbContext.Reviewers.Include(r => r.Teachers).FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null)
            {
                _logger.LogWarning("Reviewer with ID {ReviewerId} not found for removing teachers", reviewerId);
                return NotFound();
            }

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
            {
                _logger.LogWarning("User not authorized to remove teachers from reviewer of type: {Type}", reviewer.Type);
                return Forbid();
            }

            var deletedTeachers = reviewer.Teachers.Where(t => teacherIds.Contains(t.Id)).ToList();
            reviewer.Teachers = reviewer.Teachers.Where(t => !teacherIds.Contains(t.Id)).ToList();

            _logger.LogInformation("Removing {TeacherCount} teachers from reviewer {ReviewerName}", deletedTeachers.Count, reviewer.Name);
            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", deletedTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are deleted from reviewer {reviewer.Name}");
            return Ok(teacherIds);
        }

        private async Task<bool> IsUserAuthorizedForModificationAsync(GeneratorType type)
        {
            _logger.LogInformation("Checking authorization for user {User} on GeneratorType {Type}",
                User.Identity?.Name ?? "Anonymous", type);

            string? policyName = type switch
            {
                GeneratorType.FIRE_AND_FORGET => Policies.ModifyFireAndForget,
                GeneratorType.DELAYED => Policies.ModifyDelayed,
                GeneratorType.RECURRING => Policies.ModifyRecurring,
                _ => null
            };

            if (policyName is null)
            {
                _logger.LogWarning("No policy found for GeneratorType {Type}", type);
                return false;
            }

            _logger.LogInformation("Using policy {Policy} for authorization check", policyName);

            // Inject IAuthorizationService into controller via DI
            var authResult = await _authorizationService.AuthorizeAsync(User, policyName!).ConfigureAwait(false);

            _logger.LogInformation("Authorization result for user {User} on policy {Policy}: {Result}",
                User.Identity?.Name ?? "Anonymous", policyName, authResult.Succeeded ? "SUCCESS" : "FAILED");

            return authResult.Succeeded;
        }
    }
}