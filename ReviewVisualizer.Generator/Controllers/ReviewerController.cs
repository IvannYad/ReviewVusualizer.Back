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
        private static object _lock = new object();
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
            var reviewer = _dbContext.Reviewers
                .AsNoTracking()
                .Include(r => r.Teachers)
                .FirstOrDefault(r => r.Id == id);

            if (reviewer is null)
                return NotFound();

            return Ok(reviewer);
        }

        [HttpGet("type/{type:generatorType}")]
        public IActionResult GetByGeneratorType([FromRoute] GeneratorType type)
        {
            var reviewers = _dbContext.Reviewers
                .AsNoTracking()
                .Include(r => r.Teachers)
                .Where(r => r.Type == type);

            if (!reviewers.Any())
                return NotFound();

            return Ok(reviewers);
        }

        [HttpPost()]
        public async Task<IActionResult> CreateAsync([FromBody] ReviewerCreateDTO reviewerDTO)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();  // Will prompt to select a debugger (Visual Studio, etc.)
            //}
            //Console.WriteLine("Debugger is attached: " + Debugger.IsAttached);

            if (!(await IsUserAuthorizedForModificationAsync(reviewerDTO.Type)))
                return Forbid();

            var reviewer = _mapper.Map<Reviewer>(reviewerDTO);
            reviewer.Teachers = new List<Teacher>();

            // Create new reviewer in generator.
            bool success = _generatorHost.CreateReviewer(reviewer);
            //Debugger.Log(1, "Generator", $"Reviewer {reviewerDTO.Type} is created with result: {success}");

            if (success)
            {
                // Create new reviewer in database.
                _dbContext.Reviewers.Add(reviewer);
                _dbContext.SaveChanges();
            }

            return Ok(success ? reviewer : null);
        }

        [HttpDelete()]
        public async Task<IActionResult> DeleteAsync([FromQuery] int reviewerId)
        {
            //Debugger.Break();
            var reviewer = await _dbContext.Reviewers.FirstOrDefaultAsync(r => r.Id == reviewerId);
            if (reviewer is null) return Ok();

            Console.WriteLine($"reviewer: {reviewer.Name}");
            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type)))
                return Forbid();

            if (_generatorHost.DeleteReviewer(reviewer.Id))
            {
                _dbContext.Reviewers.Remove(reviewer);
                await _dbContext.SaveChangesAsync();
                return Ok();
            }

            return BadRequest($"Error occurred while deleting reviewer {reviewer.Name}");
        }

        [HttpPost("generate-review")]
        public async Task<IActionResult> Generate([FromBody] GenerateReviewRequest request)
        {
            if (!(await IsUserAuthorizedForModificationAsync(request.Type)))
                return Forbid();

            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == request.ReviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateReview(request);

            return Ok();
        }

        [HttpPost("add-teachers")]
        public async Task<IActionResult> AddTeachersAsync([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
            var reviewer = _dbContext.Reviewers.Include(r => r.Teachers).FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type)))
                return Forbid();

            if (reviewer.Teachers is null) reviewer.Teachers = new List<Teacher>();
            var newTeachersForReview = teacherIds.Where(id => !reviewer.Teachers.Any(teacher => teacher.Id == id));
            var chosenTeachers = _dbContext.Teachers.Where(t => newTeachersForReview.Contains(t.Id)).ToList();

            chosenTeachers.ForEach(t => reviewer.Teachers.Add(t));
            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", chosenTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are added to reviewer {reviewer.Name}");
            return Ok(chosenTeachers);
        }

        [HttpPost("remove-teachers")]
        public async Task<IActionResult> RemoveTeachers([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
            var reviewer = _dbContext.Reviewers.Include(r => r.Teachers).FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type)))
                return Forbid();

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