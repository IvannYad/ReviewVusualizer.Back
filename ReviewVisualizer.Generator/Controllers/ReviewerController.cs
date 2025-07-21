using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Generator.Generator;
using Microsoft.EntityFrameworkCore;

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
        private readonly IServiceProvider _services;

        public ReviewerController(ILogger<ReviewerController> logger,
            ApplicationDbContext dbContext, IMapper mapper, [FromServices]IGeneratorHost generatorHost,
            IServiceProvider services)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _generatorHost = generatorHost;
            _services = services;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var reviewers = _dbContext.Reviewers.AsNoTracking().Include(r => r.Teachers).ToList();

            return Ok(reviewers);
        }

        [HttpPost()]
        public IActionResult Create([FromBody] ReviewerCreateDTO reviewerDTO)
        {
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
        public IActionResult Delete([FromQuery] int reviewerId)
        {
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
        public IActionResult GenerateFireAndForget([FromQuery]int reviewerId)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateFireAndForget(reviewer.Id);

            return Ok();
        }

        [HttpPost("generate-delayed")]
        public IActionResult GenerateDelayed([FromQuery] int reviewerId, [FromQuery] TimeSpan delay)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateDelayed(reviewer.Id, delay);
            
            return Ok();
        }

        [HttpPost("generate-recurring")]
        public IActionResult GenerateRecurring([FromQuery] int reviewerId, [FromQuery] string cron)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            _generatorHost.GenerateRecurring(reviewer.Id, cron);

            return Ok();
        }

        [HttpPost("add-teachers")]
        public IActionResult AddTeachers([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
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
        public IActionResult StartReviewer([FromQuery] int reviewerId, [FromBody] int[] teacherIds)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == reviewerId);
            if (reviewer is null) return NotFound();

            var deletedTeachers = reviewer.Teachers.Where(t => teacherIds.Contains(t.Id)).ToList();
            reviewer.Teachers = reviewer.Teachers.Where(t => !teacherIds.Contains(t.Id)).ToList();

            _dbContext.SaveChanges();

            _logger.LogInformation($"Teachers [{string.Join(", ", deletedTeachers.Select(t => $"{t.FirstName} {t.LastName}"))}] are deleted from reviewer {reviewer.Name}");
            return Ok(teacherIds);
        }
    }
}
