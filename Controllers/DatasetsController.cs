using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Helpers;
using Pidar.Models;
using Pidar.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // ===============================================================
        // INTERNAL CHILD PROCESSOR
        // ===============================================================
        private void ProcessChild<T>(T? entity, DbSet<T> set) where T : class
        {
            if (entity == null)
                return;

            if (IsEntityEmpty(entity))
            {
                var entry = _context.Entry(entity);
                if (entry.IsKeySet)
                    set.Remove(entity);
                return;
            }

            if (_context.Entry(entity).IsKeySet)
                _context.Update(entity);
            else
                set.Add(entity);
        }

        private static bool IsEntityEmpty<T>(T entity)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                // Skip keys and navigations
                if (prop.Name == "DatasetId" || prop.Name == "Dataset")
                    continue;

                var value = prop.GetValue(entity);

                // Strings: meaningful if not empty/whitespace
                if (prop.PropertyType == typeof(string))
                {
                    if (!string.IsNullOrWhiteSpace(value as string))
                        return false;

                    continue;
                }

                // Value types: meaningful only if not default(T)
                if (prop.PropertyType.IsValueType)
                {
                    var defaultValue = Activator.CreateInstance(prop.PropertyType);
                    if (!Equals(value, defaultValue))
                        return false;

                    continue;
                }

                // Reference types (non-string): meaningful if not null
                if (value != null)
                    return false;
            }

            return true;
        }


        // ===============================================================
        // SEQUENTIAL DISPLAY ID REGENERATION (1..N ALWAYS)
        // ===============================================================
        [NonAction]
        public async Task RegenerateDisplayIdsAsync()
        {
            var all = await _context.Datasets
                .OrderBy(x => x.DatasetId)
                .ToListAsync();

            int next = 1;
            foreach (var ds in all)
            {
                ds.DisplayId = next++;
            }

            await _context.SaveChangesAsync();
        }

        // ===============================================================
        // SEARCH FORM
        // ===============================================================
        [Route("Search")]
        public async Task<IActionResult> ShowSearchForm(int pageNumber = 1)
        {
            const int pageSize = 10;

            var query = _context.Datasets
                .IncludeAll()
                .AsNoTracking()
                .OrderBy(x => x.DisplayId);

            var paginated = await PaginatedList<Dataset>
                .CreateAsync(query, pageNumber, pageSize);

            return View(paginated);
        }

        // ===============================================================
        // SEARCH RESULTS
        // ===============================================================
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(
            string? SearchPhrase,
            string? sortOrder,
            int pageNumber = 1)
        {
            const int pageSize = 10;

            ViewData["DisplayIdSortParam"] =
                sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";

            if (string.IsNullOrWhiteSpace(SearchPhrase))
                return RedirectToAction(nameof(Index));

            var allData = await _context.Datasets
                .IncludeAll()
                .AsNoTracking()
                .ToListAsync();

            var results = allData.Where(m =>
                GetAllStringValues(m)
                    .Any(v => v.Contains(SearchPhrase, System.StringComparison.OrdinalIgnoreCase))
            ).ToList();

            results = sortOrder switch
            {
                "displayid_desc" => results.OrderByDescending(m => m.DisplayId).ToList(),
                _ => results.OrderBy(m => m.DisplayId).ToList()
            };

            var page = results
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["DatasetCount"] = results.Count;
            ViewData["TotalSampleSize"] = CalculateSampleSize(results);

            var stats = GetMetadataStats();
            ViewData["TableColumnCount"] = stats.TotalFields;
            ViewBag.MetadataSections = stats.SectionCounts;

            return View("Index", new PaginatedList<Dataset>(page, results.Count, pageNumber, pageSize));
        }

        private IEnumerable<string> GetAllStringValues(Dataset ds)
        {
            foreach (var prop in typeof(Dataset).GetProperties())
            {
                if (prop.PropertyType == typeof(string) &&
                    prop.GetValue(ds) is string s &&
                    !string.IsNullOrWhiteSpace(s))
                    yield return s;
            }

            object?[] children =
            {
                ds.StudyDesign, ds.Publication, ds.StudyComponent, ds.DatasetInfo,
                ds.InVivo, ds.Procedures, ds.ImageAcquisition, ds.ImageData,
                ds.ImageCorrelation, ds.Analyzed, ds.Ontology
            };

            foreach (var child in children)
            {
                if (child == null) continue;

                foreach (var prop in child.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string) &&
                        prop.GetValue(child) is string cv &&
                        !string.IsNullOrWhiteSpace(cv))
                        yield return cv;
                }
            }
        }

        // ===============================================================
        // METADATA STATS
        // ===============================================================
        private (int TotalFields, Dictionary<string, int> SectionCounts) GetMetadataStats()
        {
            var entities = new Dictionary<string, System.Type>
            {
                {"Dataset", typeof(Dataset)},
                {"Study Design", typeof(StudyDesign)},
                {"Publication", typeof(Publication)},
                {"Study Component", typeof(StudyComponent)},
                {"Dataset Info", typeof(DatasetInfo)},
                {"In Vivo", typeof(InVivo)},
                {"Procedures", typeof(Procedures)},
                {"Image Acquisition", typeof(ImageAcquisition)},
                {"Image Data", typeof(ImageData)},
                {"Image Correlation", typeof(ImageCorrelation)},
                {"Analyzed", typeof(Analyzed)},
                {"Ontology", typeof(Ontology)}
            };

            var sections = new Dictionary<string, int>();
            var distinct = new HashSet<string>();

            foreach (var section in entities)
            {
                var entityType = _context.Model.FindEntityType(section.Value);
                if (entityType == null) continue;

                var fields = entityType
                    .GetProperties()
                    .Select(p => p.Name)
                    .Where(p => p != "DatasetId" && p != "DisplayId")
                    .ToList();

                sections[section.Key] = fields.Count;
                foreach (var f in fields) distinct.Add(f);
            }

            return (distinct.Count, sections);
        }

        // ===============================================================
        // INDEX
        // ===============================================================
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? sortOrder, int pageNumber = 1)
        {
            ViewData["ActivePage"] = "Dataset";
            ViewData["DisplayIdSortParam"] =
                sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";

            var query = _context.Datasets
                .IncludeAll()
                .AsNoTracking()
                .OrderBy(x => x.DisplayId);

            var paginated = await PaginatedList<Dataset>
                .CreateAsync(query, pageNumber, 10);

            ViewData["DatasetCount"] = await _context.Datasets.CountAsync();
            ViewData["TotalSampleSize"] = await CalculateTotalSampleSizeAsync();

            var stats = GetMetadataStats();
            ViewData["TableColumnCount"] = stats.TotalFields;
            ViewBag.MetadataSections = stats.SectionCounts;

            return View(paginated);
        }

        // ===============================================================
        // DETAILS
        // ===============================================================
        [Route("Details/{displayId}")]
        public async Task<IActionResult> Details(int displayId)
        {
            var ds = await _context.Datasets
                .IncludeAll()
                .FirstOrDefaultAsync(x => x.DisplayId == displayId);

            return ds == null ? NotFound() : View(ds);
        }

        // ===============================================================
        // CREATE (GET)
        // ===============================================================
        [Authorize]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            int nextDisplayId =
                (await _context.Datasets.MaxAsync(x => (int?)x.DisplayId) ?? 0) + 1;

            ViewBag.SuggestedDisplayId = nextDisplayId;

            return View(new DatasetCreateViewModel
            {
                Dataset = new Dataset { DisplayId = nextDisplayId },
                StudyDesign = new(),
                Publication = new(),
                StudyComponent = new(),
                DatasetInfo = new(),
                InVivo = new(),
                Procedures = new(),
                ImageAcquisition = new(),
                ImageData = new(),
                ImageCorrelation = new(),
                Analyzed = new(),
                Ontology = new()
            });
        }


        // ===============================================================
        // CREATE (POST)
        // ===============================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(DatasetCreateViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    _context.Datasets.Add(vm.Dataset);
                    await _context.SaveChangesAsync();

                    int id = vm.Dataset.DatasetId;
                    AssignFK(vm, id);

                    object[] children =
                    {
                        vm.StudyDesign!, vm.Publication!, vm.StudyComponent!,
                        vm.DatasetInfo!, vm.InVivo!, vm.Procedures!,
                        vm.ImageAcquisition!, vm.ImageData!,
                        vm.ImageCorrelation!, vm.Analyzed!, vm.Ontology!
                    };

                    _context.AddRange(children);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                });

                // ⭐ regenerate sequential DisplayIds
                await RegenerateDisplayIdsAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating dataset");
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        // ===============================================================
        // EDIT (GET)
        // ===============================================================
        [Authorize]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var ds = await _context.Datasets
                .IncludeAll()
                .FirstOrDefaultAsync(x => x.DatasetId == id);

            if (ds == null) return NotFound();

            return View(new DatasetCreateViewModel
            {
                Dataset = ds,
                StudyDesign = ds.StudyDesign ?? new(),
                Publication = ds.Publication ?? new(),
                StudyComponent = ds.StudyComponent ?? new(),
                DatasetInfo = ds.DatasetInfo ?? new(),
                InVivo = ds.InVivo ?? new(),
                Procedures = ds.Procedures ?? new(),
                ImageAcquisition = ds.ImageAcquisition ?? new(),
                ImageData = ds.ImageData ?? new(),
                ImageCorrelation = ds.ImageCorrelation ?? new(),
                Analyzed = ds.Analyzed ?? new(),
                Ontology = ds.Ontology ?? new()
            });
        }

        // ===============================================================
        // EDIT (POST)
        // ===============================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, DatasetCreateViewModel vm)
        {
            if (id != vm.Dataset.DatasetId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(vm);

            // 1) Load with tracking + children
            var entity = await _context.Datasets
                .IncludeAll()
                .FirstOrDefaultAsync(x => x.DatasetId == id);

            if (entity == null)
                return NotFound();

            // 2) Preserve DisplayId (never trust the client)
            vm.Dataset.DisplayId = entity.DisplayId;

            // 3) Ensure FKs for incoming children
            AssignFK(vm, id);

            // 4) Update root entity safely
            _context.Entry(entity).CurrentValues.SetValues(vm.Dataset);

            // 5) Update children safely (add / update / delete)
            UpdateChild(entity.StudyDesign, vm.StudyDesign, ds => entity.StudyDesign = ds);
            UpdateChild(entity.Publication, vm.Publication, p => entity.Publication = p);
            UpdateChild(entity.StudyComponent, vm.StudyComponent, sc => entity.StudyComponent = sc);
            UpdateChild(entity.DatasetInfo, vm.DatasetInfo, di => entity.DatasetInfo = di);
            UpdateChild(entity.InVivo, vm.InVivo, iv => entity.InVivo = iv);
            UpdateChild(entity.Procedures, vm.Procedures, pr => entity.Procedures = pr);
            UpdateChild(entity.ImageAcquisition, vm.ImageAcquisition, ia => entity.ImageAcquisition = ia);
            UpdateChild(entity.ImageData, vm.ImageData, idt => entity.ImageData = idt);
            UpdateChild(entity.ImageCorrelation, vm.ImageCorrelation, ic => entity.ImageCorrelation = ic);
            UpdateChild(entity.Analyzed, vm.Analyzed, an => entity.Analyzed = an);
            UpdateChild(entity.Ontology, vm.Ontology, on => entity.Ontology = on);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private void UpdateChild<T>(
    T? tracked,
    T? incoming,
    Action<T?> assignToParent
) where T : class
        {
            var hasIncoming = incoming != null && !IsEntityEmpty(incoming);

            // Nothing exists and nothing meaningful came in
            if (tracked == null && !hasIncoming)
                return;

            // Remove existing entity
            if (tracked != null && !hasIncoming)
            {
                _context.Remove(tracked);
                assignToParent(null);
                return;
            }

            // Add new entity
            if (tracked == null && hasIncoming)
            {
                _context.Add(incoming!);
                assignToParent(incoming);
                return;
            }

            // Update existing entity
            _context.Entry(tracked!).CurrentValues.SetValues(incoming!);
        }




        // ===============================================================
        // DELETE
        // ===============================================================
        [Authorize]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ds = await _context.Datasets
                .IncludeAll()
                .FirstOrDefaultAsync(x => x.DatasetId == id);

            return ds == null ? NotFound() : View(ds);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ds = await _context.Datasets.FindAsync(id);
            if (ds == null)
                return NotFound();

            _context.Datasets.Remove(ds);
            await _context.SaveChangesAsync();

            // ⭐ regenerate sequential DisplayIds
            await RegenerateDisplayIdsAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================================================
        // HELPERS
        // ===============================================================
        private void AssignFK(DatasetCreateViewModel vm, int id)
        {
            if (vm.StudyDesign != null) vm.StudyDesign.DatasetId = id;
            if (vm.Publication != null) vm.Publication.DatasetId = id;
            if (vm.StudyComponent != null) vm.StudyComponent.DatasetId = id;
            if (vm.DatasetInfo != null) vm.DatasetInfo.DatasetId = id;
            if (vm.InVivo != null) vm.InVivo.DatasetId = id;
            if (vm.Procedures != null) vm.Procedures.DatasetId = id;
            if (vm.ImageAcquisition != null) vm.ImageAcquisition.DatasetId = id;
            if (vm.ImageData != null) vm.ImageData.DatasetId = id;
            if (vm.ImageCorrelation != null) vm.ImageCorrelation.DatasetId = id;
            if (vm.Analyzed != null) vm.Analyzed.DatasetId = id;
            if (vm.Ontology != null) vm.Ontology.DatasetId = id;
        }

        private int CalculateSampleSize(List<Dataset> results)
        {
            int total = 0;

            foreach (var ds in results)
            {
                if (ds.InVivo == null) continue;
                if (int.TryParse(ds.InVivo.OverallSampleSize, out int n))
                    total += n;
            }

            return total;
        }

        private async Task<int> CalculateTotalSampleSizeAsync()
        {
            var vals = await _context.InVivos
                .Select(x => x.OverallSampleSize)
                .ToListAsync();

            int total = 0;
            foreach (var v in vals)
                if (int.TryParse(v, out int n))
                    total += n;

            return total;
        }

        private bool DatasetExists(int id)
        {
            return _context.Datasets.Any(e => e.DatasetId == id);
        }
    }

    // ===============================================================
    // INCLUDE EXTENSION
    // ===============================================================
    public static class DatasetIncludeExtensions
    {
        public static IQueryable<Dataset> IncludeAll(this IQueryable<Dataset> q)
        {
            return q.Include(x => x.StudyDesign)
                    .Include(x => x.Publication)
                    .Include(x => x.StudyComponent)
                    .Include(x => x.DatasetInfo)
                    .Include(x => x.InVivo)
                    .Include(x => x.Procedures)
                    .Include(x => x.ImageAcquisition)
                    .Include(x => x.ImageData)
                    .Include(x => x.ImageCorrelation)
                    .Include(x => x.Analyzed)
                    .Include(x => x.Ontology);
        }
    }
}
