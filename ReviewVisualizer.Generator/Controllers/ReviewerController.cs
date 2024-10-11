using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using System.Drawing;
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
        
        public ReviewerController(ILogger<ReviewerController> logger, ApplicationDbContext dbContext, IMapper mapper, [FromServices]IGeneratorHost generatorHost)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _generatorHost = generatorHost;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var teachers = _dbContext.Reviewers.Include(r => r.Teachers).ToList();

            if (teachers is null)
                return NotFound();

            return Ok(teachers);
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

        [HttpPost("stop-reviewer/{id:int}")]
        public IActionResult StopReviewer(int id)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == id);
            if (reviewer is null) return NotFound();

            // Stop reviewer in generator.
            var success = _generatorHost.StopReviewer(reviewer.Id);

            if (success)
            {
                // Update info in darabase.
                reviewer.IsStopped = true;
                _dbContext.SaveChanges();
            }

            return Ok(success);
        }

        [HttpPost("start-reviewer/{id:int}")]
        public IActionResult StartReviewer(int id)
        {
            var reviewer = _dbContext.Reviewers.FirstOrDefault(r => r.Id == id);
            if (reviewer is null) return NotFound();

            // Start reviewer in generator.
            bool success = _generatorHost.StartReviewer(reviewer.Id);

            if (success)
            {
                // Update info in darabase.
                reviewer.IsStopped = false;
                _dbContext.SaveChanges();
            }

            return Ok(success);
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
