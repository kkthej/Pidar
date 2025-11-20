using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;

namespace Pidar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatasetsApiController : ControllerBase
    {
        private readonly PidarDbContext _context;

        public DatasetsApiController(PidarDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Dataset>>> GetDatasets()
        {
            return await _context.Datasets.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Dataset>> GetDataset(int id)
        {
            var dataset = await _context.Datasets
                .Include(d => d.StudyDesign)
                .FirstOrDefaultAsync(x => x.DatasetId == id);

            if (dataset == null)
                return NotFound();

            return dataset;
        }
    }
}
