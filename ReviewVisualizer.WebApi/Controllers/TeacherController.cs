using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Mapper;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data.Enums;
using System.Runtime.CompilerServices;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("teachers")]
    public class TeacherController : ControllerBase
    {
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
        public IActionResult GetAll()
        {
            var teachers = _dbContext.Teachers.ToList();

            return Ok(teachers);
        }

        [HttpGet("get-for-department/{deptId:int}")]
        public IActionResult GetAllForDepartment(int deptId)
        {
            var teachers = _dbContext.Teachers.Where(t => t.DepartmentId == deptId).ToList();

            if (teachers is null)
                return NotFound();

            return Ok(teachers);
        }

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var teacher = _dbContext.Teachers.FirstOrDefault(d => d.Id == id);

            if (teacher is null)
                return NotFound();

            return Ok(teacher);
        }

        [HttpPost()]
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
        public IActionResult Delete([FromQuery] int teacherId)
        {
            Teacher? teacher = _dbContext.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (teacher is null)
            {
                return NotFound();
            }

            lock (_lock)
            {
                if (CheckIfTeacherIsUnderReview(teacher))
                {
                    return BadRequest("Cannot delete teacher. It is under active review");
                }

                Review[] reviews = _dbContext.Reviews.Where(r => r.TeacherId == teacher.Id).ToArray();
                _dbContext.Reviews.RemoveRange(reviews);

                DeleteImage(teacher.PhotoUrl);
                _dbContext.Teachers.Remove(teacher);
                _dbContext.SaveChanges();
            }
            
            return Ok();
        }

        [HttpPost("upload-image")]
        public IActionResult UploadImage([FromForm] IFormFile deptIcon)
        {
            string name = $"teachers_{Guid.NewGuid().ToString()}_{deptIcon.FileName}";
            using var memoryStream = new MemoryStream();
            deptIcon.CopyTo(memoryStream);

            Image image = Image.FromStream(memoryStream);
            image.Save(Path.Combine(_imagesStoragePath, name), System.Drawing.Imaging.ImageFormat.Png);

            return new ContentResult()
            {
                Content = name,
                ContentType = "application/json"
            };
        }

        [HttpPost("delete-image")]
        public IActionResult DeleteImage([FromQuery] string imgName)
        {
            try
            {
                string imgFullPath = Path.Combine(_imagesStoragePath, imgName);
                if (System.IO.File.Exists(imgFullPath))
                {
                    System.IO.File.Delete(imgFullPath);
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("get-department-rank/{teacherId:int}")]
        public  IActionResult GetRankInDepartment(int teacherId)
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
	                        WHERE t.DepartmentId = {teacher.DepartmentId}
                        ) tr
                        WHERE tr.Id = {teacher.Id}
                    ").First();
                return Ok(rank);
            }
        }

        [HttpGet("get-global-rank/{teacherId:int}")]
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
                    WHERE t.Id = {teacherId}
                ";

            lock (_lock)
            {
                double? grade = _dbContext.Database.SqlQuery<double?>(FormattableStringFactory.Create(query)).FirstOrDefault();

                grade = grade is not null ? (double)Math.Round((decimal)grade, 2) : null;
                return Ok(grade);
            }
        }

        [HttpGet("get-top")]
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

        #region == Helper methods ==

        private bool CheckIfTeacherIsUnderReview(Teacher teacher)
        {
            var reviewers = _dbContext.Reviewers.Where(r => r.Teachers.Any(t => t.Id == teacher.Id));

            return reviewers?.Any(r => !r.IsStopped) ?? false;
        }
        #endregion
    }
}
