using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Generator.Generator;
using Microsoft.EntityFrameworkCore;
using Autofac;
using Microsoft.AspNetCore.Authorization;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data.Enums;

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
            using var scope = _container.BeginLifetimeScope();
            var dbContext = scope.Resolve<ApplicationDbContext>();
            var reviewers = dbContext.Reviewers.AsNoTracking().Include(r => r.Teachers).ToList();

            return Ok(reviewers);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateAsync([FromBody] ReviewerCreateDTO reviewerDTO)
        {
            if (!(await IsUserAuthorizedForModificationAsync(reviewerDTO.Type)))
                return Forbid();

            var reviewer = _mapper.Map<Reviewer>(reviewerDTO);
            reviewer.Teachers = new List<Teacher>();

            // Create new reviewer in generator.
            bool success = _generatorHost.CreateReviewer(reviewer);

            if (success)
            {
                // Create new reviewer in database.
                _dbContext.Reviewers.Add(reviewer);
                _dbContext.SaveChanges();
            }
            
            return Ok(success);
        }

        [HttpDelete()]
        public async Task<IActionResult> DeleteAsync([FromQuery] int reviewerId, [FromQuery] GeneratorType type)
        {
            if (!(await IsUserAuthorizedForModificationAsync(type)))
                return Forbid();

            Reviewer? reviewer = _dbContext.Reviewers.FirstOrDefault(t => t.Id == reviewerId);
            if (reviewer is null)
            {
                return Ok();
            }

            if (_generatorHost.DeleteReviewer(reviewer.Id))
            {
                _dbContext.Reviewers.Remove(reviewer);
                _dbContext.SaveChanges();
                return Ok();
            }
            
            return BadRequest($"Error occurred while deleting reviewer {reviewer.Name}");
        }

        [HttpPost("generate-fire-and-forget")]
        public async Task<IActionResult> GenerateFireAndForgetAsync([FromQuery]int reviewerId)
        {
            if (!(await IsUserAuthorizedForModificationAsync(GeneratorType.FIRE_AND_FORGET)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateFireAndForget(reviewer.Id);

            return Ok();
        }

        [HttpPost("generate-delayed")]
        public async Task<IActionResult> GenerateDelayedAsync([FromQuery] int reviewerId, [FromQuery] TimeSpan delay)
        {
            if (!(await IsUserAuthorizedForModificationAsync(GeneratorType.DELAYED)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateDelayed(reviewer.Id, delay);
            
            return Ok();
        }

        [HttpPost("generate-recurring")]
        public async Task<IActionResult> GenerateRecurringAsync([FromQuery] int reviewerId, [FromQuery] string cron)
        {
            if (!(await IsUserAuthorizedForModificationAsync(GeneratorType.RECURRING)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateRecurring(reviewer.Id, cron);

            return Ok();
        }

        [HttpPost("add-teachers")]
        public async Task<IActionResult> AddTeachersAsync([FromQuery] int reviewerId, [FromQuery] GeneratorType type, [FromBody] int[] teacherIds)
        {
            if (!(await IsUserAuthorizedForModificationAsync(type)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            if(reviewer.Teachers is null) reviewer.Teachers = new List<Teacher>();
            var newTeachersForReview = teacherIds.Where(id => !reviewer.Teachers.Any(teacher => teacher.Id == id));
            var chosenTeachers = _dbContext.Teachers.Where(t => newTeachersForReview.Contains(t.Id)).ToList();

            chosenTeachers.ForEach(t => reviewer.Teachers.Add(t));
            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", chosenTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are added to reviewer {reviewer.Name}");
            return Ok(chosenTeachers);
        }

        [HttpPost("remove-teachers")]
        public async Task<IActionResult> StartReviewerAsync([FromQuery] int reviewerId, [FromQuery] GeneratorType type, [FromBody] int[] teacherIds)
        {
            if (!(await IsUserAuthorizedForModificationAsync(type)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            var deletedTeachers = reviewer.Teachers.Where(t => teacherIds.Contains(t.Id)).ToList();
            reviewer.Teachers = reviewer.Teachers.Where(t => !teacherIds.Contains(t.Id)).ToList();

            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", deletedTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are deleted from reviewer {reviewer.Name}");
            return Ok(teacherIds);
        }

        private async Task<bool> IsUserAuthorizedForModificationAsync(GeneratorType type)
        {
            string? policyName = type switch
            {
                GeneratorType.FIRE_AND_FORGET => Policies.ModifyFireAndForget,
                GeneratorType.DELAYED => Policies.ModifyDelayed,
                GeneratorType.RECURRING => Policies.ModifyRecurring,
                _ => null
            };

            if (policyName is null)
                return false;

            // Inject IAuthorizationService into controller via DI
            var authResult = await _authorizationService.AuthorizeAsync(User, policyName!);

            return authResult.Succeeded;
        }
    }
}
