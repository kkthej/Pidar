using Microsoft.AspNetCore.Mvc;
using Pidar.Data;
using Pidar.Models;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;

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

        public async Task<IActionResult> Statistic()
        {
            ViewData["ActivePage"] = "Statistic";

            // Total dataset count
            ViewData["DatasetCount"] = await _context.Dataset.CountAsync();

            // Total sample size
            var sampleSizes = await _context.Dataset
                .Select(x => x.OverallSampleSize)
                .ToListAsync();

            int totalSampleSize = 0;
            foreach (var size in sampleSizes)
            {
                if (!string.IsNullOrWhiteSpace(size) &&
                    int.TryParse(size.Replace(",", ""), out int parsed))
                {
                    totalSampleSize += parsed;
                }
            }
            ViewData["TotalSampleSize"] = totalSampleSize;

            // Table column count (safe null handling)
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            int columnCount = entityType?.GetProperties()?.Count() ?? 0;
            ViewData["TableColumnCount"] = columnCount;

            // -------- Statistics & Charts ----------------------------------------------------

            // Imaging Modality Distribution
            var modalitiesRaw = await _context.Dataset
                .Where(d => !string.IsNullOrWhiteSpace(d.ImagingModality))
                .Select(d => d.ImagingModality!)
                .ToListAsync();

            var modalityCounts = modalitiesRaw
                .SelectMany(m => m.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(m => m.Trim().ToUpperInvariant())
                .GroupBy(m => m)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewData["ModalityDistribution"] = JsonSerializer.Serialize(modalityCounts);


            // Country Distribution
            ViewData["CountryDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrWhiteSpace(m.CountryOfImagingFacility))
                    .GroupBy(m => m.CountryOfImagingFacility)
                    .Select(g => new { Country = g.Key!, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            // Disease Model Distribution
            ViewData["DiseaseModelDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrWhiteSpace(m.DiseaseModel))
                    .GroupBy(m => m.DiseaseModel)
                    .Select(g => new { Disease = g.Key!, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            // Organ/Tissue distribution
            var organsRaw = await _context.Dataset
                .Where(d => !string.IsNullOrWhiteSpace(d.OrganOrTissue))
                .Select(d => d.OrganOrTissue!)
                .ToListAsync();

            var organCounts = organsRaw
                .SelectMany(o => o.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(o => o.Trim())
                .GroupBy(o => o)
                .Select(g => new { Organ = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            ViewData["OrganDistribution"] = JsonSerializer.Serialize(organCounts);

            // Yearly uploads
            ViewData["YearlyUploads"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => m.UpdatedYear != null)
                    .GroupBy(m => m.UpdatedYear)
                    .Select(g => new { Year = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ToListAsync());

            // Status distribution
            ViewData["StatusDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrWhiteSpace(m.Status))
                    .GroupBy(m => m.Status)
                    .Select(g => new { Status = g.Key!, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync());

            return View();
        }

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

        [HttpGet]
        [Route("Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                errorModel.ErrorMessage = exceptionFeature.Error.Message;

                _logger.LogError(exceptionFeature.Error, "Unhandled exception occurred.");
            }

            if (statusCode.HasValue)
            {
                errorModel.ErrorMessage ??= $"Status Code: {statusCode}";
            }

            return View(errorModel);
        }
    }
}
