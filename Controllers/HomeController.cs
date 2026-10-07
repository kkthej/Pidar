using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Mapping;
using Pidar.Models;
using Pidar.Models.Ontology;
using Pidar.Models.Summaries;
using Pidar.Models.Xnat;
using Pidar.Services.Xnat;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;


namespace Pidar.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PidarDbContext _context;

        private readonly IConfiguration _config;
        private readonly IXnatMultiService _xnatMulti;
        private readonly IHttpClientFactory _httpClientFactory;


        public HomeController(ILogger<HomeController> logger, PidarDbContext context,
    IConfiguration config, IXnatMultiService xnatMulti, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _context = context;
            _config = config;

            _xnatMulti = xnatMulti;
            _httpClientFactory = httpClientFactory;
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

            // One small row per dataset; all counting is done in Helpers/StatisticsBuilder.cs
            var rows = await _context.Datasets.AsNoTracking()
                .Select(d => new Pidar.Helpers.StatisticsBuilder.Row(
                    d.StudyComponent != null ? d.StudyComponent.ImagingModality : null,
                    d.InVivo != null ? d.InVivo.DiseaseCategory : null,
                    d.InVivo != null ? d.InVivo.OrganOrTissue : null,
                    d.InVivo != null ? d.InVivo.Species : null,
                    d.InVivo != null ? d.InVivo.OverallSampleSize : null,
                    d.InVivo != null ? d.InVivo.Sex : null,
                    d.DatasetInfo != null ? d.DatasetInfo.CountryOfImagingFacility : null,
                    d.DatasetInfo != null ? d.DatasetInfo.DatasetAccess : null,
                    d.Analyzed != null ? d.Analyzed.Status : null,
                    d.Analyzed != null ? d.Analyzed.UpdatedYear : null,
                    d.Publication != null ? d.Publication.PaperLinked : null))
                .ToListAsync();

            return View(Pidar.Helpers.StatisticsBuilder.Build(rows));
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

        public async Task<IActionResult> Xnat(CancellationToken ct)
        {
            ViewData["ActivePage"] = "Xnat";
            ViewBag.XnatBaseUrl = _config["Xnat:BaseUrl"]!.TrimEnd('/');
            ViewBag.XnatInstances = _config.GetSection("XnatInstances")
                                  .Get<List<XnatInstanceOptions>>() ?? new();
            ViewBag.CartoApiKey = _config["Maps:CartoApiKey"] ?? "";
            try
            {
                var projects = await _xnatMulti.GetAllPublicProjectsAsync(ct);
                return View(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load XNAT projects.");
                return View(new List<XnatProjectsApiResponse.ProjectRow>());
            }
        }

        // ============================================================
        // XNAT DATA SUMMARY ENDPOINT
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> XnatSummary(string instanceKey, string projectId, CancellationToken ct)
        {
            var instances = _config.GetSection("XnatInstances").Get<List<XnatInstanceOptions>>() ?? new();
            var inst = instances.FirstOrDefault(x => x.Key == instanceKey);
            if (inst == null) return NotFound();

            var baseUrl = inst.BaseUrl.TrimEnd('/');
            var url =
                $"{baseUrl}/data/projects/{Uri.EscapeDataString(projectId)}/resources/metadata/files/{Uri.EscapeDataString(projectId)}.json";

            using var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrWhiteSpace(inst.Username) && !string.IsNullOrWhiteSpace(inst.Password))
            {
                var token = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{inst.Username}:{inst.Password}"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", token);
            }

            var resp = await client.GetAsync(url, ct);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync(ct);

            var summary = XnatMetadataSummaryMapper.FromMetadataJson(json);

            return Ok(summary);
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

                sectionCounts[section.Key] = fields.Count;

                foreach (var f in fields)
                    distinctFields.Add(f);
            }

            return (distinctFields.Count, sectionCounts);
        }
    }
}