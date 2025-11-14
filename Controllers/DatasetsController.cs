using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidar.Data;
using Pidar.Models;
using System.Diagnostics;

namespace Pidar.Controllers
{
    [Route("Datasets")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DatasetsController : Controller
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<DatasetsController> _logger;

        public DatasetsController(PidarDbContext context, ILogger<DatasetsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ------------------------------------------------------------
        // INDEX
        // ------------------------------------------------------------
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? sortOrder, int pageNumber = 1)
        {
            ViewData["ActivePage"] = "Dataset";
            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";
            ViewBag.CurrentSort = sortOrder;

            var query = _context.Dataset.AsQueryable();

            query = sortOrder switch
            {
                "displayid_desc" => query.OrderByDescending(m => m.DisplayId),
                "displayid_asc" => query.OrderBy(m => m.DisplayId),
                _ => query.OrderBy(m => m.DisplayId)
            };

            const int pageSize = 10;
            var paginatedData = await PaginatedList<Dataset>.CreateAsync(query, pageNumber, pageSize);

            ViewData["DatasetCount"] = await _context.Dataset.CountAsync();
            ViewData["TotalSampleSize"] = await CalculateTotalSampleSizeAsync();
            ViewData["TableColumnCount"] = GetTableColumnCount();

            return View(paginatedData);
        }

        // ------------------------------------------------------------
        // SEARCH FORM
        // ------------------------------------------------------------
        [Route("Search")]
        public async Task<IActionResult> ShowSearchForm(int pageNumber = 1)
        {
            const int pageSize = 10;
            var query = _context.Dataset.AsQueryable();
            var paginatedData = await PaginatedList<Dataset>.CreateAsync(query, pageNumber, pageSize);
            return View(paginatedData);
        }

        // ------------------------------------------------------------
        // SEARCH RESULTS
        // ------------------------------------------------------------
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(string? SearchPhrase, string? sortOrder, int pageNumber = 1)
        {
            const int pageSize = 10;

            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";
            ViewBag.CurrentSort = sortOrder;

            if (!string.IsNullOrWhiteSpace(SearchPhrase))
            {
                var allData = await _context.Dataset.ToListAsync();

                var results = allData.Where(m =>
                    typeof(Dataset).GetProperties()
                        .Where(p => p.PropertyType == typeof(string))
                        .Select(p => p.GetValue(m)?.ToString())
                        .Any(value => value is not null &&
                            value.Contains(SearchPhrase, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                results = sortOrder switch
                {
                    "displayid_desc" => results.OrderByDescending(m => m.DisplayId).ToList(),
                    _ => results.OrderBy(m => m.DisplayId).ToList()
                };

                var totalRecords = results.Count;
                var paginatedResults = results.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                ViewData["DatasetCount"] = totalRecords;
                ViewData["TotalSampleSize"] = CalculateSampleSize(results);
                ViewData["TableColumnCount"] = GetTableColumnCount();

                return View("Index", new PaginatedList<Dataset>(paginatedResults, totalRecords, pageNumber, pageSize));
            }

            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------
        // DETAILS
        // ------------------------------------------------------------
        [Route("Details")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            Dataset? dataset = await _context.Dataset
                .FirstOrDefaultAsync(m => m.DisplayId == id);

            if (dataset == null)
                return NotFound();

            return View(dataset);
        }

        // ------------------------------------------------------------
        // CREATE (GET)
        // ------------------------------------------------------------
        [Authorize]
        [Route("Create")]
        public IActionResult Create()
        {
            int? maxDisplayId = _context.Dataset.Max(m => (int?)m.DisplayId);
            ViewBag.SuggestedDisplayId = (maxDisplayId ?? 0) + 1;
            return View();
        }

        // ------------------------------------------------------------
        // CREATE (POST)
        // ------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Create")]
        public async Task<IActionResult> Create(Dataset dataset)
        {
            if (!ModelState.IsValid)
                return View(dataset);

            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    _context.Add(dataset);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                });

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is Npgsql.PostgresException pg)
            {
                ModelState.AddModelError("", $"Database error ({pg.SqlState}): {pg.MessageText}");
                _logger.LogError(ex, "DB Error during CREATE");
                return View(dataset);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogError(ex, "General Error during CREATE");
                return View(dataset);
            }
        }

        // ------------------------------------------------------------
        // EDIT (GET)
        // ------------------------------------------------------------
        [Authorize]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            Dataset? dataset = await _context.Dataset.FindAsync(id);

            if (dataset == null)
                return NotFound();

            return View(dataset);
        }

        // ------------------------------------------------------------
        // EDIT (POST)
        // ------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, Dataset dataset)
        {
            if (id != dataset.DatasetId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(dataset);

            try
            {
                _context.Update(dataset);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DatasetExists(id))
                    return NotFound();
                throw;
            }
        }

        // ------------------------------------------------------------
        // DELETE (GET)
        // ------------------------------------------------------------
        [Authorize]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            Dataset? dataset = await _context.Dataset
                .FirstOrDefaultAsync(m => m.DatasetId == id);

            if (dataset == null)
                return NotFound();

            return View(dataset);
        }

        // ------------------------------------------------------------
        // DELETE (POST)
        // ------------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult result = RedirectToAction(nameof(Index));

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    Dataset? dataset = await _context.Dataset.FindAsync(id);
                    if (dataset == null)
                    {
                        result = NotFound();
                        await transaction.RollbackAsync();
                        return;
                    }

                    int deletedDisplayId = dataset.DisplayId;

                    _context.Dataset.Remove(dataset);
                    await _context.SaveChangesAsync();

                    var recordsToUpdate = await _context.Dataset
                        .Where(m => m.DisplayId > deletedDisplayId)
                        .OrderBy(m => m.DisplayId)
                        .ToListAsync();

                    foreach (var record in recordsToUpdate)
                    {
                        record.DisplayId -= 1;
                        _context.Update(record);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteConfirmed");
                result = View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "An error occurred while deleting the record."
                });
            }

            return result;
        }

        // ------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------
        private async Task<int> CalculateTotalSampleSizeAsync()
        {
            var sampleSizes = await _context.Dataset
                .Select(x => x.OverallSampleSize)
                .ToListAsync();

            int total = 0;
            foreach (var size in sampleSizes)
            {
                if (!string.IsNullOrWhiteSpace(size) &&
                    int.TryParse(size.Replace(",", ""), out int parsed))
                {
                    total += parsed;
                }
            }
            return total;
        }

        private int CalculateSampleSize(List<Dataset> items)
        {
            int total = 0;

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.OverallSampleSize) &&
                    int.TryParse(item.OverallSampleSize.Replace(",", ""), out int parsed))
                {
                    total += parsed;
                }
            }
            return total;
        }

        private int GetTableColumnCount()
        {
            var entityType = _context.Model.FindEntityType(typeof(Dataset));
            return entityType?.GetProperties()?.Count() ?? 0;
        }

        private bool DatasetExists(int id)
        {
            return _context.Dataset.Any(e => e.DatasetId == id);
        }
    }
}
