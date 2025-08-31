using Autofac;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
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
    public class ReviewerController : ControllerBase
    {
        private readonly ILogger<ReviewerController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IGeneratorHost _generatorHost;
        private readonly ILifetimeScope _container;
        private readonly IAuthorizationService _authorizationService;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public ReviewerController(ILifetimeScope container, IAuthorizationService authorizationService, IServiceProvider services
            , IConfiguration configuration)
        {
            _container = container;

            var scope = _container.BeginLifetimeScope();

            _dbContext = scope.Resolve<ApplicationDbContext>();
            _logger = scope.Resolve<ILogger<ReviewerController>>();
            _mapper = scope.Resolve<IMapper>();
            _generatorHost = scope.Resolve<IGeneratorHost>();
            _authorizationService = authorizationService;
            _services = services;
            _configuration = configuration;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            try
            {
                var dataProtection = _services.GetRequiredService<IDataProtectionProvider>();
                var protector = dataProtection.CreateProtector("test");
                var encrypted = protector.Protect("test");
                var decrypted = protector.Unprotect(encrypted);

                var cookies = HttpContext.Request.Cookies;
                var user = HttpContext.User;
                var reviewers = _dbContext.Reviewers.AsNoTracking().Include(r => r.Teachers).ToList();
                return Ok(new
                {
                    Encrypted = encrypted,
                    Decrypted = decrypted,
                    User = user,
                    Cookies = cookies,
                    Configuration = new
                    {
                        CookieDomain = _configuration["AuthCookieSettings:Domain"],
                        DataProtectionUrl = _configuration["DataProtection:Url"],
                        DataProtectionContainer = _configuration["DataProtection:ContainerName"],
                        ApplicationName = _configuration["ApplicationName"]
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

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

            if (!(await IsUserAuthorizedForModificationAsync(reviewerDTO.Type).ConfigureAwait(false)))
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
            var reviewer = await _dbContext.Reviewers.FirstOrDefaultAsync(r => r.Id == reviewerId).ConfigureAwait(false);
            if (reviewer is null) return Ok();

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
                return Forbid();

            if (_generatorHost.DeleteReviewer(reviewer.Id))
            {
                _dbContext.Reviewers.Remove(reviewer);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                return Ok();
            }

            return BadRequest($"Error occurred while deleting reviewer {reviewer.Name}");
        }

        [HttpPost("generate-review")]
        public async Task<IActionResult> Generate([FromBody] GenerateReviewRequest request)
        {
            if (!(await IsUserAuthorizedForModificationAsync(request.Type).ConfigureAwait(false)))
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

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
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

            if (!(await IsUserAuthorizedForModificationAsync(reviewer.Type).ConfigureAwait(false)))
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
            var authResult = await _authorizationService.AuthorizeAsync(User, policyName!).ConfigureAwait(false);

            return authResult.Succeeded;
        }
    }
}