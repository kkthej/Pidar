using Microsoft.AspNetCore.Mvc;
using Pidar.Data;
using Pidar.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Make sure this is present
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;       // For Task<T> return types

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

            // Get dataset count
            ViewData["DatasetCount"] = await _context.Dataset.CountAsync();

            // Get total sample size with stats
            var sampleSizes = await _context.Dataset
                .Select(x => x.OverallSampleSize)
                .ToListAsync();

            int totalSampleSize = 0;
            foreach (var size in sampleSizes)
            {
                if (int.TryParse(size?.Replace(",", ""), out int parsedSize))
                {
                    totalSampleSize += parsedSize;
                }
            }
            ViewData["TotalSampleSize"] = totalSampleSize;

            // Get table column count
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            ViewData["TableColumnCount"] = entityType?.GetProperties().Count() ?? 0;

            // Get data for charts
            // Fetch all imaging modality entries (non-empty)
            var modalitiesRaw = await _context.Dataset
                .Where(d => !string.IsNullOrEmpty(d.ImagingModality))
                .Select(d => d.ImagingModality)
                .ToListAsync();

            // Split only by commas, trim, normalize, and count each modality
            var modalityCounts = modalitiesRaw
                .SelectMany(m => m.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(m => m.Trim().ToUpperInvariant())
                .GroupBy(m => m)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewData["ModalityDistribution"] = JsonSerializer.Serialize(modalityCounts);


            ViewData["CountryDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrEmpty(m.CountryOfImagingFacility))
                    .GroupBy(m => m.CountryOfImagingFacility)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            ViewData["DiseaseModelDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrEmpty(m.DiseaseModel))
                    .GroupBy(m => m.DiseaseModel)
                    .Select(g => new { Disease = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            // Fetch all OrganOrTissue entries (non-empty)
            var organsRaw = await _context.Dataset
                .Where(d => !string.IsNullOrEmpty(d.OrganOrTissue))
                .Select(d => d.OrganOrTissue)
                .ToListAsync();

            // Split only by commas, trim, normalize, and count
            var organCounts = organsRaw
                .SelectMany(o => o.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(o => o.Trim())
                .GroupBy(o => o)
                .Select(g => new { Organ = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            ViewData["OrganDistribution"] = JsonSerializer.Serialize(organCounts);


            ViewData["YearlyUploads"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => m.UpdatedYear != null)
                    .GroupBy(m => m.UpdatedYear)
                    .Select(g => new { Year = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ToListAsync());

            ViewData["StatusDistribution"] = JsonSerializer.Serialize(
                await _context.Dataset
                    .Where(m => !string.IsNullOrEmpty(m.Status))
                    .GroupBy(m => m.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
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

            // Get the exception details if available
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionHandlerPathFeature?.Error != null)
            {
                errorModel.ErrorMessage = exceptionHandlerPathFeature.Error.Message;

                // Log the error if needed
                _logger.LogError(exceptionHandlerPathFeature.Error,
                    "Unhandled exception occurred");
            }

            if (statusCode.HasValue)
            {
                errorModel.ErrorMessage ??= $"Status Code: {statusCode}";
            }

            return View(errorModel);
        }
    }
}