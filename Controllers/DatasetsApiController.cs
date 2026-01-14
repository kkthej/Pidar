using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models.Summaries;

namespace Pidar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatasetsApiController : ControllerBase
    {
        private readonly PidarDbContext _context;

        public DatasetsApiController(PidarDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DatasetSummary>>> GetDatasets()
        {
            var items = await _context.Datasets
                .AsNoTracking()
                .OrderBy(d => d.DisplayId)
                .Select(d => new DatasetSummary
                {
                    DisplayId = d.DisplayId,
                    Species = d.InVivo != null ? d.InVivo.Species : null,
                    OrganOrTissue = d.InVivo != null ? d.InVivo.OrganOrTissue : null,
                    DiseaseModel = d.InVivo != null ? d.InVivo.DiseaseModel : null,
                    SampleSize = d.InVivo != null ? d.InVivo.OverallSampleSize : null,
                    ImagingModality = d.StudyComponent != null ? d.StudyComponent.ImagingModality : null
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DatasetSummary>> GetDataset(int id)
        {
            var item = await _context.Datasets
                .AsNoTracking()
                .Where(d => d.DatasetId == id)
                .Select(d => new DatasetSummary
                {
                    
                    DisplayId = d.DisplayId,
                    Species = d.InVivo != null ? d.InVivo.Species : null,
                    OrganOrTissue = d.InVivo != null ? d.InVivo.OrganOrTissue : null,
                    DiseaseModel = d.InVivo != null ? d.InVivo.DiseaseModel : null,
                    SampleSize = d.InVivo != null ? d.InVivo.OverallSampleSize : null,
                    ImagingModality = d.StudyComponent != null ? d.StudyComponent.ImagingModality : null
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound();

            return Ok(item);
        }
    }
}
