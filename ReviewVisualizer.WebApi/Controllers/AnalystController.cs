using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ReviewVisualizer.Data;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;
using Microsoft.EntityFrameworkCore;
using ReviewVisualizer.WebApi.Processor;

namespace ReviewVisualizer.Generator.Controllers
{
    [ApiController]
    [Route("analysts")]
    public class AnalystController : ControllerBase
    {
        private readonly ILogger<AnalystController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IProcessorHost _processorHost;
        private readonly IQueueController _queue;

        public AnalystController(ILogger<AnalystController> logger,
            ApplicationDbContext dbContext,
            IMapper mapper,
            [FromServices] IProcessorHost processorHost,
            [FromServices] IQueueController queue)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _processorHost = processorHost;
            _queue = queue;
        }

        [HttpGet()]
        public IActionResult GetAll()
        {
            var analysts = _dbContext.Analysts.ToList();

            return Ok(analysts);
        }

        [HttpGet("get-queue-size")]
        public IActionResult GetQueueSize()
        {
            var size = _queue.GetQueueSize();

            return Ok(size);
        }

        [HttpPost()]
        public IActionResult Create([FromBody] AnalystCreateDTO analystDTO)
        {
            var analyst = _mapper.Map<Analyst>(analystDTO);
            
            // Create new analyst in processor.
            bool success = _processorHost.CreateAnalyst(analyst);

            if (success)
            {
                // Create new analyst in database.
                _dbContext.Analysts.Add(analyst);
                _dbContext.SaveChanges();
            }
            
            return Ok(success);
        }

        [HttpPost("stop-analyst/{id:int}")]
        public IActionResult StopAnalyst(int id)
        {
            var analyst = _dbContext.Analysts.FirstOrDefault(a => a.Id == id);
            if (analyst is null) return NotFound();

            // Stop analyst in generator.
            var success = _processorHost.StopAnalyst(analyst.Id);

            if (success)
            {
                // Update info in darabase.
                analyst.IsStopped = true;
                _dbContext.SaveChanges();
            }

            return Ok(success);
        }

        [HttpPost("start-analyst/{id:int}")]
        public IActionResult StartAnalyst(int id)
        {
            var analyst = _dbContext.Analysts.FirstOrDefault(a => a.Id == id);
            if (analyst is null) return NotFound();

            // Start analyst in generator.
            bool success = _processorHost.StartAnalyst(analyst.Id);

            if (success)
            {
                // Update info in darabase.
                analyst.IsStopped = false;
                _dbContext.SaveChanges();
            }

            return Ok(success);
        }
    }
}
