using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // EPPlus
using Pidar.Data;
using Pidar.Models.Ontology;
using Pidar.Services;


namespace Pidar.Controllers;

// IMPORTANT: lock this down. If you don't have roles, keep [Authorize] at least.
[Authorize]
[Route("admin/ontology")]
public sealed class AdminOntologyController : Controller
{
    private readonly PidarDbContext _db;
    private readonly OntologyIndexService _indexService;

    public AdminOntologyController(PidarDbContext db, OntologyIndexService indexService)
    {
        _db = db;
        _indexService = indexService;
    }

    /// <summary>
    /// One-time backfill: rebuild dataset_ontology_term for all datasets.
    /// GET /admin/ontology/rebuild-terms
    /// </summary>
    [HttpGet("rebuild-terms")]
    public async Task<IActionResult> RebuildTerms()
    {
        var ids = await _db.Datasets
            .AsNoTracking()
            .Select(d => d.DatasetId)
            .ToListAsync();

        var rebuilt = 0;
        foreach (var id in ids)
        {
            await _indexService.RebuildAsync(id);
            rebuilt++;
        }

        var termCount = await _db.DatasetOntologyTerms.CountAsync();
        return Ok(new
        {
            message = "Rebuild completed",
            datasetsProcessed = rebuilt,
            totalTerms = termCount
        });
    }

    /// <summary>
    /// One-time import from Excel: ontology_export_synonyms.xlsx (Synonyms separated by ';')
    /// GET /admin/ontology/import-synonyms
    /// </summary>
    [HttpGet("import-synonyms")]
    public async Task<IActionResult> ImportSynonyms()
    {
        // Path in your dev machine: adjust if you store the file elsewhere.
        // If you want, we can turn this into an upload endpoint instead.
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "ontology_export_synonyms.xlsx");

        if (!System.IO.File.Exists(filePath))
            return BadRequest(new { error = "Excel file not found", expectedPath = filePath });

   


        // Load existing pairs (dedupe)
        var existing = await _db.OntologySynonyms
            .AsNoTracking()
            .Select(x => new { x.Code, x.Synonym })
            .ToListAsync();

        var existingSet = new HashSet<string>(
            existing.Select(x => $"{x.Code}||{x.Synonym}".ToLowerInvariant())
        );

        var toInsert = new List<OntologySynonym>();

        using var package = new ExcelPackage(new FileInfo(filePath));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws == null)
            return BadRequest(new { error = "No worksheet found in file" });

        // Assumption: first row is headers, and columns are:
        // A: OntologyCode, B: Synonyms (semicolon separated)
        // If your Excel differs, tell me the column names/order and I'll adjust.
        var startRow = 2;
        var lastRow = ws.Dimension.End.Row;

        for (var r = startRow; r <= lastRow; r++)
        {
            var code = ws.Cells[r, 1].GetValue<string>()?.Trim();
            var synRaw = ws.Cells[r, 2].GetValue<string>()?.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(synRaw))
                continue;

            var synonyms = synRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var s in synonyms)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;

                var key = $"{code}||{s}".ToLowerInvariant();
                if (!existingSet.Add(key)) continue; // already exists

                toInsert.Add(new OntologySynonym
                {
                    Code = code,
                    Synonym = s
                });
            }
        }

        if (toInsert.Count == 0)
            return Ok(new { message = "No new synonyms to insert", inserted = 0 });

        _db.OntologySynonyms.AddRange(toInsert);
        await _db.SaveChangesAsync();

        var total = await _db.OntologySynonyms.CountAsync();
        return Ok(new
        {
            message = "Synonym import completed",
            inserted = toInsert.Count,
            totalSynonyms = total
        });
    }

    /// <summary>
    /// Quick check endpoint
    /// GET /admin/ontology/status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var termCount = await _db.DatasetOntologyTerms.CountAsync();
        var synonymCount = await _db.OntologySynonyms.CountAsync();

        return Ok(new
        {
            datasetOntologyTerms = termCount,
            ontologySynonyms = synonymCount
        });
    }
}
