using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Mapper;
using System.Drawing;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("teachers")]
    public class TeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly string _imagesStoragePath;

        public TeacherController(IConfiguration configuration, ApplicationDbContext dbContext, IMapper mapper)
        {
            _imagesStoragePath = configuration.GetValue<string>("ImagesStorage");
            _dbContext = dbContext;
            _mapper = mapper;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var teachers = _dbContext.Teachers.ToList();

            if (teachers is null)
                return NotFound();

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
            _dbContext.Teachers.Add(_mapper.Map<Teacher>(dept));
            _dbContext.SaveChanges();
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
    }
}
