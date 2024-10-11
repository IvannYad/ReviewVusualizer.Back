using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;
using System.Drawing;
using System.IO;

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
            _dbContext = dbContext;
            _mapper = mapper;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var departments = _dbContext.Departments.ToList();

            if (departments is null)
                return NotFound();

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
    }
}
