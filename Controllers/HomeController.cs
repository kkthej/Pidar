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
            return RedirectToAction("Index", "Metadatas");
        }

        public async Task<IActionResult> Statistic()
        {
            ViewData["ActivePage"] = "Statistic";

            // Get metadata count
            ViewData["MetadataCount"] = await _context.Metadata.CountAsync();

            // Get total sample size with stats
            var sampleSizes = await _context.Metadata
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
            var entityType = _context.Model.FindEntityType(typeof(Metadata));
            ViewData["TableColumnCount"] = entityType?.GetProperties().Count() ?? 0;

            // Get data for charts
            ViewData["ModalityDistribution"] = JsonSerializer.Serialize(
                await _context.Metadata
                    .Where(m => !string.IsNullOrEmpty(m.ImagingModality))
                    .GroupBy(m => m.ImagingModality)
                    .Select(g => new { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync());

            ViewData["CountryDistribution"] = JsonSerializer.Serialize(
                await _context.Metadata
                    .Where(m => !string.IsNullOrEmpty(m.CountryOfImagingFacility))
                    .GroupBy(m => m.CountryOfImagingFacility)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            ViewData["DiseaseModelDistribution"] = JsonSerializer.Serialize(
                await _context.Metadata
                    .Where(m => !string.IsNullOrEmpty(m.DiseaseModel))
                    .GroupBy(m => m.DiseaseModel)
                    .Select(g => new { Disease = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            ViewData["OrganDistribution"] = JsonSerializer.Serialize(
                await _context.Metadata
                    .Where(m => !string.IsNullOrEmpty(m.OrganOrTissue))
                    .GroupBy(m => m.OrganOrTissue)
                    .Select(g => new { Organ = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync());

            ViewData["YearlyUploads"] = JsonSerializer.Serialize(
                await _context.Metadata
                    .Where(m => m.UpdatedYear != null)
                    .GroupBy(m => m.UpdatedYear)
                    .Select(g => new { Year = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ToListAsync());

            ViewData["StatusDistribution"] = JsonSerializer.Serialize(
                await _context.Metadata
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