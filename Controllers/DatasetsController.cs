using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;
using Pidar.Models.ViewModels;
using Pidar.Helpers;

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
        // INTERNAL CHILD PROCESSOR (NO ObjectHelper)
        // ===============================================================
        private void ProcessChild<T>(T? entity, DbSet<T> set) where T : class
        {
            if (entity == null)
                return;

            // Check if all string properties are empty → delete child
            if (IsEntityEmpty(entity))
            {
                var entry = _context.Entry(entity);
                if (entry.IsKeySet)
                    set.Remove(entity);

                return;
            }

            // Not empty → Insert or Update
            if (_context.Entry(entity).IsKeySet)
                _context.Update(entity);
            else
                set.Add(entity);
        }

        private static bool IsEntityEmpty<T>(T entity)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? val = prop.GetValue(entity)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return false;
                }
                else
                {
                    var val = prop.GetValue(entity);
                    if (val != null)
                        return false;
                }
            }
            return true;
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
                .OrderBy(x => x.DisplayId)
                .AsQueryable();

            var paginatedData = await PaginatedList<Dataset>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedData);
        }

        // ===============================================================
        // SEARCH RESULTS
        // ===============================================================
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(string? SearchPhrase, string? sortOrder, int pageNumber = 1)
        {
            const int pageSize = 10;

            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc"
                ? "displayid_desc"
                : "displayid_asc";

            ViewBag.CurrentSort = sortOrder;

            if (string.IsNullOrWhiteSpace(SearchPhrase))
                return RedirectToAction(nameof(Index));

            // Load everything (Dataset + 11 child tables)
            var allData = await _context.Datasets
                .IncludeAll()
                .AsNoTracking()
                .ToListAsync();

            // Flatten + Search through ALL string properties from ALL tables
            var results = allData.Where(m =>
                GetAllStringValues(m)
                    .Any(value => value.Contains(SearchPhrase, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // Sorting
            results = sortOrder switch
            {
                "displayid_desc" => results.OrderByDescending(m => m.DisplayId).ToList(),
                _ => results.OrderBy(m => m.DisplayId).ToList()
            };

            // Pagination
            var totalRecords = results.Count;
            var paginatedResults = results
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["DatasetCount"] = totalRecords;
            ViewData["TotalSampleSize"] = CalculateSampleSize(results);
            // ⭐ FIX INCLUDED HERE ⭐
            var stats = GetMetadataStats();
            ViewData["TableColumnCount"] = stats.TotalFields;
            ViewBag.MetadataSections = stats.SectionCounts;

            return View("Index", new PaginatedList<Dataset>(paginatedResults, totalRecords, pageNumber, pageSize));
        }
        // Collect ALL string values from Dataset + all children
        private IEnumerable<string> GetAllStringValues(Dataset ds)
        {
            foreach (var prop in typeof(Dataset).GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    string? val = prop.GetValue(ds)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        yield return val;
                }
            }

            // Child entities
            var children = new object?[]
            {
        ds.StudyDesign,
        ds.Publication,
        ds.StudyComponent,
        ds.DatasetInfo,
        ds.InVivo,
        ds.Procedures,
        ds.ImageAcquisition,
        ds.ImageData,
        ds.ImageCorrelation,
        ds.Analyzed,
        ds.Ontology
            };

            foreach (var child in children)
            {
                if (child == null) continue;

                foreach (var prop in child.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        string? val = prop.GetValue(child)?.ToString();
                        if (!string.IsNullOrWhiteSpace(val))
                            yield return val;
                    }
                }
            }
        }

        // ------------------------------------------
        // COUNT DISTINCT METADATA FIELDS + SECTIONS
        // ------------------------------------------
        private (int TotalFields, Dictionary<string, int> SectionCounts) GetMetadataStats()
        {
            var entities = new Dictionary<string, Type>
        {
            { "Dataset", typeof(Dataset) },
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
                    .Where(p => !p.Equals("DatasetId", StringComparison.OrdinalIgnoreCase) &&
                                !p.Equals("dataset_id", StringComparison.OrdinalIgnoreCase) &&
                                !p.Equals("DisplayId", StringComparison.OrdinalIgnoreCase))  // exclude DisplayId too
                    .ToList();

                // Count ONLY real metadata fields for this section
                sectionCounts[section.Key] = fields.Count;

                // Add to distinct global list
                foreach (var f in fields)
                    distinctFields.Add(f);
            }

            return (distinctFields.Count, sectionCounts);
        }



        // ===============================================================
        // INDEX
        // ===============================================================
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? sortOrder, int pageNumber = 1)
        {
            ViewData["ActivePage"] = "Dataset";
            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc"
                ? "displayid_desc"
                : "displayid_asc";

            // ⭐⭐ THE FIX — LOAD ALL CHILD TABLES ⭐⭐
            var query = _context.Datasets
                .IncludeAll()
                .AsNoTracking()
                .AsQueryable();

            query = sortOrder switch
            {
                "displayid_desc" => query.OrderByDescending(x => x.DisplayId),
                "displayid_asc" => query.OrderBy(x => x.DisplayId),
                _ => query.OrderBy(x => x.DisplayId)
            };

            var paginated = await PaginatedList<Dataset>.CreateAsync(query, pageNumber, 10);

            // ----- Stats -----
            ViewData["DatasetCount"] = await _context.Datasets.CountAsync();
            ViewData["TotalSampleSize"] = await CalculateTotalSampleSizeAsync();

            // 🔥 NEW metadata stats
            var stats = GetMetadataStats();
            ViewData["TableColumnCount"] = stats.TotalFields;   // global field count
            ViewBag.MetadataSections = stats.SectionCounts;     // per-section count

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
        public IActionResult Create()
        {
            int nextId = (_context.Datasets.Max(x => (int?)x.DisplayId) ?? 0) + 1;
            ViewBag.SuggestedDisplayId = nextId;

            return View(new DatasetCreateViewModel
            {
                Dataset = new Dataset { DisplayId = nextId },
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

                    // Always insert children on Create
                    // Force compiler to treat all children as non-null
                    object[] children =
                    {
                        vm.StudyDesign!,
                        vm.Publication!,
                        vm.StudyComponent!,
                        vm.DatasetInfo!,
                        vm.InVivo!,
                        vm.Procedures!,
                        vm.ImageAcquisition!,
                        vm.ImageData!,
                        vm.ImageCorrelation!,
                        vm.Analyzed!,
                        vm.Ontology!
                    };

                    _context.AddRange(children);


                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                });

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
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

            if (ds == null)
                return NotFound();

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

            AssignFK(vm, id);

            try
            {
                _context.Update(vm.Dataset);

                // Process children safely
                ProcessChild(vm.StudyDesign, _context.StudyDesigns);
                ProcessChild(vm.Publication, _context.Publications);
                ProcessChild(vm.StudyComponent, _context.StudyComponents);
                ProcessChild(vm.DatasetInfo, _context.DatasetInfos);
                ProcessChild(vm.InVivo, _context.InVivos);
                ProcessChild(vm.Procedures, _context.Procedures);
                ProcessChild(vm.ImageAcquisition, _context.ImageAcquisitions);
                ProcessChild(vm.ImageData, _context.ImageDatas);
                ProcessChild(vm.ImageCorrelation, _context.ImageCorrelations);
                ProcessChild(vm.Analyzed, _context.Analyzed);
                ProcessChild(vm.Ontology, _context.Ontologies);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                if (!DatasetExists(id))
                    return NotFound();
                throw;
            }
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
                if (ds.InVivo == null)
                    continue;

                var size = ds.InVivo.OverallSampleSize;

                if (int.TryParse(size, out int n))
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

        private int GetTableColumnCount()
        {
            return _context.Model.FindEntityType(typeof(Dataset))?
                .GetProperties().Count() ?? 0;
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
