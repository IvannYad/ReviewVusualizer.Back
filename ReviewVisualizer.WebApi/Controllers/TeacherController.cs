using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.Drawing;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("teachers")]
    public class TeacherController : ControllerBase
    {
        private string[] permittedPhotoExtensions = { ".png", ".jpg" };

        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly string _imagesStoragePath;
        private static object _lock = new object();

        public TeacherController(IConfiguration configuration, ApplicationDbContext dbContext, IMapper mapper)
        {
            _imagesStoragePath = configuration.GetValue<string>("ImagesStorage");
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
            _mapper = mapper;
        }

        [HttpGet()]
        [Produces("application/json", "application/xml")]
        public IActionResult GetAll()
        {
            var teachers = _dbContext.Teachers.ToList();

            return Ok(teachers);
        }

        [HttpGet("get-for-department/{deptId:int}")]
        [Produces("application/json", "application/xml")]
        public IActionResult GetAllForDepartment(int deptId)
        {
            var teachers = _dbContext.Teachers.Where(t => t.DepartmentId == deptId).ToList();

            if (teachers is null)
                return NotFound();

            return Ok(teachers);
        }

        [HttpGet("{id:int}")]
        [Produces("application/json")]
        public IActionResult Get(int id)
        {
            var teacher = _dbContext.Teachers.FirstOrDefault(d => d.Id == id);

            if (teacher is null)
                return NotFound();

            return Ok(teacher);
        }

        [HttpPost()]
        [Produces("application/json")]
        [Consumes("application/json")] // request must have Content-Type headers, otherwise 415 Unsupported Media Type
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult Create([FromBody] TeacherCreateDTO dept)
        {
            lock (_lock)
            {
                _dbContext.Teachers.Add(_mapper.Map<Teacher>(dept));
                _dbContext.SaveChanges();
                return Ok();
            }
        }

        [HttpDelete()]
        [Produces("application/json")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult Delete([FromQuery] int teacherId)
        {
            Teacher? teacher = _dbContext.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                return NotFound();
            }

            lock (_lock)
            {
                Review[] reviews = _dbContext.Reviews.Where(r => r.TeacherId == teacher.Id).ToArray();
                _dbContext.Reviews.RemoveRange(reviews);

                DeleteImage(teacher.PhotoUrl);
                _dbContext.Teachers.Remove(teacher);
                _dbContext.SaveChanges();
            }

            return Ok();
        }

        [HttpPost("upload-image")]
        [Produces("application/json")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult UploadImage([FromForm] IFormFile deptIcon)
        {
            var ext = Path.GetExtension(deptIcon.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !permittedPhotoExtensions.Contains(ext))
                return BadRequest("Invalid photo file extension");

            // Generate new image name. Cannot use name supplied by used due to security reasons.
            string name = $"teachers_{Guid.NewGuid()}_{deptIcon.FileName}";
            using var memoryStream = new MemoryStream();
            deptIcon.CopyTo(memoryStream);

            Image image = Image.FromStream(memoryStream);
            image.Save(Path.Combine(_imagesStoragePath, name), System.Drawing.Imaging.ImageFormat.Png);

            return Ok(name);
        }

        [HttpPost("delete-image")]
        [Produces("application/json")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult DeleteImage([FromQuery] string imgName)
        {
            string imgFullPath = Path.Combine(_imagesStoragePath, imgName);
            if (System.IO.File.Exists(imgFullPath))
            {
                System.IO.File.Delete(imgFullPath);
            }

            return Ok();
        }

        [HttpGet("get-department-rank/{teacherId:int}")]
        [Produces("application/json")]
        public IActionResult GetRankInDepartment(int teacherId)
        {
            var teacher = _dbContext.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                return NotFound();
            }

            lock (_lock)
            {
                int rank = (int)_dbContext.Database.SqlQueryRaw<long>(
                    @$"
                        SELECT TOP 1 tr.rank as Value
                        FROM (
                            SELECT t.Id, ROW_NUMBER() OVER(ORDER BY t.Rating DESC) as rank
                            FROM Teachers t
	                        WHERE t.DepartmentId = @departmentId
                        ) tr
                        WHERE tr.Id = @teacherId
                    ", [
                        new SqlParameter("departmentId", teacher.DepartmentId),
                        new SqlParameter("teacherId", teacher.Id)
                ]).First();
                return Ok(rank);
            }
        }

        [HttpGet("get-global-rank/{teacherId:int}")]
        [Produces("application/json")]
        public IActionResult GetGlobalRank(int teacherId)
        {
            var teacher = _dbContext.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                return NotFound();
            }

            lock (_lock)
            {
                int rank = (int)_dbContext.Database.SqlQuery<long>(
                    @$"
                        SELECT TOP 1 tr.rank as Value
                        FROM (
                            SELECT t.Id, ROW_NUMBER() OVER(ORDER BY t.Rating DESC) as rank
                            FROM Teachers t
                        ) tr
                        WHERE tr.Id = {teacherId}
                    ").First();

                return Ok(rank);
            }
        }

        [HttpGet("get-grade/{teacherId:int}")]
        [Produces("application/json")]
        public IActionResult GetGrade(int teacherId, [FromQuery] GradeCategory category)
        {
            var teacher = _dbContext.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                return NotFound();
            }

            string columnName = Enum.GetName(typeof(GradeCategory), category) ?? "Overall";
            string query = @$"
                    SELECT AVG(CAST(r.{columnName} AS FLOAT)) as Value
                    FROM Teachers t
                    LEFT JOIN Reviews r ON r.TeacherId = t.Id
                    WHERE t.Id = @teacherId
                ";

            lock (_lock)
            {
                double? grade = _dbContext.Database.SqlQueryRaw<double?>(query, new SqlParameter("@teacherId", teacher.Id)).FirstOrDefault();

                grade = grade is not null ? (double)Math.Round((decimal)grade, 2) : null;
                return Ok(grade);
            }
        }

        [HttpGet("get-top")]
        [Produces("application/json")]
        public IActionResult GetTop10()
        {
            var teachers = _dbContext.Teachers
                .OrderByDescending(t => t.Rating)
                .Take(10)
                .ToList();

            if (teachers is null)
                return NotFound();

            return Ok(teachers);
        }

        [HttpGet("get-top-in-department/{deptId:int}")]
        [Produces("application/json")]
        public IActionResult GetTop10InDepartment(int deptId)
        {
            var teachers = _dbContext.Teachers
                .Where(t => t.DepartmentId == deptId)
                .OrderByDescending(t => t.Rating)
                .Take(10)
                .ToList();

            if (teachers is null)
                return NotFound();

            return Ok(teachers);
        }

        [HttpGet("get-best")]
        [Produces("application/json")]
        public IActionResult GetBest()
        {
            var teacher = _dbContext.Teachers.AsEnumerable().MaxBy(t => t.Rating);

            if (teacher is null)
                return NotFound();

            return Ok(teacher);
        }
    }
}