using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;

namespace Pidar.Controllers
{
    [Route("api/metadata")]
    [ApiController]
    public class MetadatasApiController : ControllerBase
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<MetadatasApiController> _logger;

        public MetadatasApiController(PidarDbContext context, ILogger<MetadatasApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Fetches all metadata records.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMetadata()
        {
            
            try
            {
                var metadataList = await _context.Metadata.ToListAsync();
                return Ok(metadataList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching metadata");
                return StatusCode(500, "An error occurred while fetching metadata.");
            }
        }

        /// <summary>
        /// Fetches a specific metadata record by DatasetId.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMetadataById(int id)
        {
            try
            {
                var metadata = await _context.Metadata.FindAsync(id);
                if (metadata == null)
                {
                    return NotFound();
                }
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching metadata with ID {Id}", id);
                return StatusCode(500, "An error occurred while fetching metadata.");
            }

           
        }

    }
}
