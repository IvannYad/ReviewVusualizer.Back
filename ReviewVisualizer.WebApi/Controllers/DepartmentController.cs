using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly string _imagesStoragePath;

        public DepartmentController(IConfiguration configuration, ApplicationDbContext dbContext, IMapper mapper)
        {
            _imagesStoragePath = configuration.GetValue<string>("ImagesStorage");
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
            _mapper = mapper;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var departments = _dbContext.Departments.ToList();

            return Ok(departments);
        }

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var department = _dbContext.Departments.FirstOrDefault(d => d.Id == id);

            if (department is null)
                return NotFound();

            return Ok(department);
        }

        [HttpPost()]
        public IActionResult Create([FromBody] DepartmentCreateDTO dept)
        {
            _dbContext.Departments.Add(_mapper.Map<Department>(dept));
            _dbContext.SaveChanges();
            return Ok();
        }

        [HttpPost("upload-image")]
        public IActionResult UploadImage([FromForm] IFormFile deptIcon)
        {
            string name = $"departments_{Guid.NewGuid().ToString()}_{deptIcon.FileName}";
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

        [HttpGet("get-grade/{departmentId:int}")]
        public IActionResult GetGrade(int departmentId, [FromQuery] GradeCategory category)
        {
            var department = _dbContext.Departments.FirstOrDefault(d => d.Id == departmentId);
            if (department is null)
            {
                return NotFound();
            }

            string columnName = Enum.GetName(typeof(GradeCategory), category) ?? "Overall";
            string query = 
                @$"
                    SELECT AVG(CAST(r.{columnName} AS FLOAT)) as Value
                    FROM Departments d
                    LEFT JOIN Teachers t ON t.DepartmentId = d.Id
                    LEFT JOIN Reviews r ON r.TeacherId = t.Id
                    WHERE d.Id = {department.Id}
                ";
            double? grade = _dbContext.Database.SqlQuery<double?>(FormattableStringFactory.Create(query)).FirstOrDefault();

            grade = grade is not null ? (double)Math.Round((decimal)grade, 2) : null; ;
            return Ok(grade);
        }

        [HttpGet("get-top")]
        public IActionResult GetTop10()
        {
            var departments = _dbContext.Departments
                .OrderByDescending(d => d.Rating)
                .Take(10)
                .ToList();

            if (departments is null)
                return NotFound();

            return Ok(departments);
        }


        [HttpGet("get-best")]
        public IActionResult GetBest()
        {
            var department = _dbContext.Departments.AsEnumerable().MaxBy(d => d.Rating);

            if (department is null)
                return NotFound();

            return Ok(department);
        }
    }
}
