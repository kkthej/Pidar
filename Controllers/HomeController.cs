using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Pidar.Data;
using Pidar.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Pidar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PidarDbContext _context;

        public HomeController(ILogger<HomeController> logger, PidarDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Datasets");
        }

        // ============================================================
        // STATISTICS PAGE
        // ============================================================
        public async Task<IActionResult> Statistic()
        {
            ViewData["ActivePage"] = "Statistic";

            // ------------------------------
            // BASIC COUNTS
            // ------------------------------
            ViewData["DatasetCount"] = await _context.Datasets.CountAsync();

            // Total sample size (InVivo)
            var sampleSizes = await _context.InVivos
                .Select(v => v.OverallSampleSize)
                .ToListAsync();

            int totalSampleSize = 0;
            foreach (var s in sampleSizes)
            {
                if (!string.IsNullOrWhiteSpace(s) &&
                    int.TryParse(s.Replace(",", ""), out int parsed))
                    totalSampleSize += parsed;
            }
            ViewData["TotalSampleSize"] = totalSampleSize;

            // Column count of Dataset table only (not sub-tables)
            var datasetEntityType = _context.Model.FindEntityType(typeof(Dataset));
            ViewData["TableColumnCount"] = datasetEntityType?.GetProperties().Count() ?? 0;





            // ============================================================
            // -------------------- CHART DATA -----------------------------
            // ============================================================


            // ------------------------------------------------------------
            // 1. IMAGING MODALITY (StudyComponent.ImagingModality)
            // ------------------------------------------------------------
            var modalityRaw = await _context.StudyComponents
                .Where(sc => sc.ImagingModality != null && sc.ImagingModality.Trim() != "")
                .Select(sc => sc.ImagingModality!)
                .ToListAsync();

            var modalityCounts = modalityRaw
                .SelectMany(m => m.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(m => m.Trim().ToUpperInvariant())
                .Where(m => m != "")
                .GroupBy(m => m)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewData["ModalityDistribution"] = JsonSerializer.Serialize(modalityCounts);


            // ------------------------------------------------------------
            // 2. COUNTRY OF IMAGING FACILITY (DatasetInfo)
            // ------------------------------------------------------------
            var countryCounts = await _context.DatasetInfos
                .Where(i => !string.IsNullOrWhiteSpace(i.CountryOfImagingFacility)) // Cleaner check
                .GroupBy(i => i.CountryOfImagingFacility) // Grouping by the raw column is index-friendly
                .Select(g => new {
                        Country = g.Key.Trim(), // Trim here, after the heavy lifting is done
                        Count = g.Count()
                    })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .AsNoTracking() //Performance Tip: Tells EF not to track these for changes
                .ToListAsync();

            ViewData["CountryDistribution"] = JsonSerializer.Serialize(countryCounts);


            // ------------------------------------------------------------
            // 3. DISEASE MODEL (InVivo)
            // ------------------------------------------------------------
            var diseaseCounts = await _context.InVivos
                .Where(v => v.DiseaseModel != null && v.DiseaseModel.Trim() != "")
                .GroupBy(v => v.DiseaseModel!.Trim())
                .Select(g => new { Disease = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewData["DiseaseModelDistribution"] = JsonSerializer.Serialize(diseaseCounts);


            // ------------------------------------------------------------
            // 4. ORGAN / TISSUE (InVivo)
            // ------------------------------------------------------------
            var organsRaw = await _context.InVivos
                .Where(v => v.OrganOrTissue != null && v.OrganOrTissue.Trim() != "")
                .Select(v => v.OrganOrTissue!)
                .ToListAsync();

            var organCounts = organsRaw
                .SelectMany(o => o.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(o => o.Trim())
                .Where(o => o != "")
                .GroupBy(o => o)
                .Select(g => new { Organ = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            ViewData["OrganDistribution"] = JsonSerializer.Serialize(organCounts);


            // ------------------------------------------------------------
            // 5. YEARLY UPLOADS (Analyzed.UpdatedYear)
            // ------------------------------------------------------------
            var yearlyUploads = await _context.Analyzed
                .Where(a => a.UpdatedYear != null)
                .GroupBy(a => a.UpdatedYear)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year)
                .ToListAsync();

            ViewData["YearlyUploads"] = JsonSerializer.Serialize(yearlyUploads);


            // ------------------------------------------------------------
            // 6. STATUS DISTRIBUTION (Analyzed.Status)
            // ------------------------------------------------------------
            var statusCounts = await _context.Analyzed
                .Where(a => a.Status != null && a.Status.Trim() != "")
                .GroupBy(a => a.Status!.Trim())
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            ViewData["StatusDistribution"] = JsonSerializer.Serialize(statusCounts);
            // ------------------------------------------------------------
            // 7. DISTRIBUTION OF METADATA
            // ------------------------------------------------------------

            var stats = GetMetadataStats();
            ViewBag.MetadataSections = stats.SectionCounts;


            return View();
        }


        // -------------------------
        // OTHER PAGES
        // -------------------------
        public IActionResult Contribute()
        {
            ViewData["ActivePage"] = "Contribute";
            return View();
        }

        public IActionResult About()
        {
            ViewData["ActivePage"] = "About";
            return View();
        }

        public IActionResult Download()
        {
            ViewData["ActivePage"] = "Download";
            return View();
        }

        public IActionResult Xnat()
        {
            ViewData["ActivePage"] = "Xnat";
            return View();
        }


        // -------------------------
        // ERROR HANDLER
        // -------------------------
        [HttpGet]
        [Route("Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (feature?.Error != null)
            {
                model.ErrorMessage = feature.Error.Message;
                _logger.LogError(feature.Error, "Unhandled exception occurred.");
            }

            if (statusCode.HasValue)
                model.ErrorMessage ??= $"Status Code: {statusCode}";

            return View(model);
        }

        // --------------------------------------------------------------
        // COUNT ALL DISTINCT METADATA FIELDS (ACROSS ALL 11 SUB TABLES)
        // --------------------------------------------------------------
        private (int TotalFields, Dictionary<string, int> SectionCounts) GetMetadataStats()
        {
            var entities = new Dictionary<string, Type>
    {
        { "Study Design", typeof(StudyDesign) },
        { "Publication", typeof(Publication) },
        { "Study Component", typeof(StudyComponent) },
        { "Dataset Info", typeof(DatasetInfo) },
        { "In Vivo", typeof(InVivo) },
        { "Procedures", typeof(Procedures) },
        { "Image Acquisition", typeof(ImageAcquisition) },
        { "Image Data", typeof(ImageData) },
        { "Image Correlation", typeof(ImageCorrelation) },
        { "Analyzed", typeof(Analyzed) },
        { "Ontology", typeof(Ontology) }
    };

            var sectionCounts = new Dictionary<string, int>();
            var distinctFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in entities)
            {
                var entityType = _context.Model.FindEntityType(section.Value);
                if (entityType == null)
                    continue;

                var fields = entityType
                    .GetProperties()
                    .Select(p => p.Name)
                    .Where(p => !p.Equals("DatasetId", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // count fields in this section
                sectionCounts[section.Key] = fields.Count;

                // add to global distinct set
                foreach (var f in fields)
                    distinctFields.Add(f);
            }

            return (distinctFields.Count, sectionCounts);
        }

    }
}
