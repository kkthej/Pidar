using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;

namespace Pidar.Controllers
{
    [Route("api/dataset")]
    [ApiController]
    public class DatasetsApiController : ControllerBase
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<DatasetsApiController> _logger;

        public DatasetsApiController(PidarDbContext context, ILogger<DatasetsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Fetches all dataset records.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDataset()
        {
            
            try
            {
                var datasetList = await _context.Dataset.ToListAsync();
                return Ok(datasetList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dataset");
                return StatusCode(500, "An error occurred while fetching dataset.");
            }
        }

        /// <summary>
        /// Fetches a specific dataset record by DatasetId.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDatasetById(int id)
        {
            try
            {
                var dataset = await _context.Dataset.FindAsync(id);
                if (dataset == null)
                {
                    return NotFound();
                }
                return Ok(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dataset with ID {Id}", id);
                return StatusCode(500, "An error occurred while fetching dataset.");
            }

           
        }

    }
}
