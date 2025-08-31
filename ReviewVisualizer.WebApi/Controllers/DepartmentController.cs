using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.AuthLibrary;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Enums;
using ReviewVisualizer.Data.Models;
using VisualizerProject;

namespace ReviewVisualizer.WebApi.Controllers
{
    [ApiController]
    [Route("departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ImageService _imageService;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public DepartmentController(IConfiguration configuration, ApplicationDbContext dbContext, IMapper mapper
            , ImageService imageService, IServiceProvider services)
        {
            _imageService = imageService;
            _dbContext = ApplicationDbContext.CreateNew(dbContext);
            _mapper = mapper;
            _configuration = configuration;
            _services = services;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            //var departments = _dbContext.Departments.ToList();

            //return Ok(departments);

            try
            {
                var dataProtection = _services.GetRequiredService<IDataProtectionProvider>();
                var protector = dataProtection.CreateProtector("test");
                var encrypted = protector.Protect("test");
                var decrypted = protector.Unprotect(encrypted);

                var cookies = HttpContext.Request.Cookies;
                var user = HttpContext.User;
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
            var department = _dbContext.Departments.FirstOrDefault(d => d.Id == id);

            if (department is null)
                return NotFound();

            return Ok(department);
        }

        [HttpPost()]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public IActionResult Create([FromBody] DepartmentCreateDTO dept)
        {
            _dbContext.Departments.Add(_mapper.Map<Department>(dept));
            _dbContext.SaveChanges();
            return Ok();
        }

        [HttpPost("upload-image")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public async Task<IActionResult> UploadImageAsync([FromForm] IFormFile deptIcon)
        {
            if (!_imageService.ValidateImage(deptIcon))
                return BadRequest("Invalid photo file extension");

            string name = await _imageService.UploadImageAsync(deptIcon);

            return new ContentResult()
            {
                Content = name,
                ContentType = "application/json"
            };
        }

        [HttpPost("delete-image")]
        [Authorize(Policy = Policies.RequireAnalyst)]
        public async Task<IActionResult> DeleteImageAsync([FromQuery] string imgName)
        {
            await _imageService.DeleteImageAsync(imgName);
            return Ok();
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
            string sql = $@"
                SELECT AVG(CAST(r.{columnName} AS FLOAT)) as Value
                FROM Departments d
                LEFT JOIN Teachers t ON t.DepartmentId = d.Id
                LEFT JOIN Reviews r ON r.TeacherId = t.Id
                WHERE d.Id = @departmentId
            ";

            // Parameterize department.Id
            var grade = _dbContext.Database
                .SqlQueryRaw<double?>(sql, new SqlParameter("@departmentId", department.Id))
                .FirstOrDefault();
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