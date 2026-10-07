using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Helpers;
using Pidar.Infrastructure;
using Pidar.Models.Ontology;

namespace Pidar.Controllers;

/// <summary>
/// Manage the ontology_synonym table (used by the search bar to find datasets by
/// alternative names, e.g. "brain" → UBERON:0000955). Admins and Curators.
/// </summary>
[Authorize(Roles = AppRoles.Editors)]
[Route("Synonyms")]
public sealed class SynonymsController : Controller
{
    private readonly PidarDbContext _db;
    private readonly ILogger<SynonymsController> _logger;

    public SynonymsController(PidarDbContext db, ILogger<SynonymsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public sealed record SynonymItem(int Id, string Text);
    public sealed record CodeGroup(string Code, string? Url, int DatasetCount, List<SynonymItem> Synonyms);
    public sealed record UnusedCode(string Code, string? Url, int DatasetCount, string Categories);
    public sealed record PageModel(string? Query, List<CodeGroup> Groups, List<UnusedCode> CodesWithoutSynonyms,
                                   int TotalCodes, int TotalSynonyms, List<string> KnownCodes);

    // GET /Synonyms?q=brain
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        q = q?.Trim();

        var all = await _db.OntologySynonyms.AsNoTracking()
            .OrderBy(s => s.Code).ThenBy(s => s.Synonym)
            .ToListAsync();

        // How many datasets use each code (codes compared upper-case)
        var termRows = await _db.DatasetOntologyTerms.AsNoTracking()
            .Select(t => new { t.Code, t.DatasetId, t.Category })
            .ToListAsync();
        var usage = termRows
            .GroupBy(t => t.Code.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => (Datasets: g.Select(x => x.DatasetId).Distinct().Count(),
                                            Categories: string.Join(", ", g.Select(x => Pretty(x.Category)).Distinct())));

        var groups = all
            .GroupBy(s => s.Code.Trim().ToUpperInvariant())
            .Select(g => new CodeGroup(
                g.Key,
                UrlFor(g.Key),
                usage.TryGetValue(g.Key, out var u) ? u.Datasets : 0,
                g.Select(s => new SynonymItem(s.Id, s.Synonym)).ToList()))
            .ToList();

        var codesWithSynonyms = groups.Select(g => g.Code).ToHashSet();
        var withoutSynonyms = usage
            .Where(kv => !codesWithSynonyms.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.Datasets).ThenBy(kv => kv.Key)
            .Select(kv => new UnusedCode(kv.Key, UrlFor(kv.Key), kv.Value.Datasets, kv.Value.Categories))
            .ToList();

        var totalCodes = groups.Count;
        var totalSynonyms = all.Count;

        if (!string.IsNullOrEmpty(q))
        {
            groups = groups
                .Where(g => g.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            g.Synonyms.Any(s => s.Text.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            withoutSynonyms = withoutSynonyms
                .Where(c => c.Code.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            c.Categories.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var known = codesWithSynonyms.Concat(usage.Keys).Distinct().OrderBy(c => c).ToList();

        return View(new PageModel(q, groups, withoutSynonyms, totalCodes, totalSynonyms, known));
    }

    // POST /Synonyms/Add  — code + one or more synonyms (new lines or ';')
    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string code, string synonyms, string? q)
    {
        var normalized = NormalizeCode(code);
        if (normalized == null)
        {
            TempData["Error"] = $"\"{code}\" is not a valid ontology code. Use the form PREFIX:ID, e.g. UBERON:0000955 or NCIT:C16809.";
            return Back(q);
        }

        var items = SplitSynonyms(synonyms);
        if (items.Count == 0)
        {
            TempData["Error"] = "Enter at least one synonym.";
            return Back(q, normalized);
        }

        var existing = await _db.OntologySynonyms
            .Where(s => s.Code.ToUpper() == normalized)
            .Select(s => s.Synonym)
            .ToListAsync();
        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var added = new List<string>();
        foreach (var s in items)
        {
            if (!existingSet.Add(s)) continue;
            _db.OntologySynonyms.Add(new OntologySynonym { Code = normalized, Synonym = s });
            added.Add(s);
        }
        await _db.SaveChangesAsync();

        var skipped = items.Count - added.Count;
        _logger.LogInformation("{User} added synonyms to {Code}: {Synonyms}", User.Identity?.Name, normalized, string.Join("; ", added));
        TempData["Message"] = added.Count == 0
            ? $"Nothing added: all synonyms already exist for {normalized}."
            : $"Added {added.Count} synonym{(added.Count == 1 ? "" : "s")} to {normalized}" +
              (skipped > 0 ? $" ({skipped} already existed)." : ".");
        return Back(q, normalized);
    }

    // POST /Synonyms/Update
    [HttpPost("Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string synonym, string? q)
    {
        var row = await _db.OntologySynonyms.FindAsync(id);
        if (row == null) return NotFound();

        var text = CleanSynonym(synonym);
        if (text == null)
        {
            TempData["Error"] = "A synonym can't be empty.";
            return Back(q, row.Code);
        }

        var duplicate = await _db.OntologySynonyms.AnyAsync(s =>
            s.Id != id && s.Code.ToUpper() == row.Code.ToUpper() && s.Synonym.ToLower() == text.ToLower());
        if (duplicate)
        {
            TempData["Error"] = $"\"{text}\" already exists for {row.Code}.";
            return Back(q, row.Code);
        }

        var old = row.Synonym;
        row.Synonym = text;
        await _db.SaveChangesAsync();

        _logger.LogInformation("{User} renamed synonym {Id} of {Code}: '{Old}' -> '{New}'", User.Identity?.Name, id, row.Code, old, text);
        TempData["Message"] = $"Updated \"{old}\" → \"{text}\".";
        return Back(q, row.Code);
    }

    // POST /Synonyms/Delete
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? q)
    {
        var row = await _db.OntologySynonyms.FindAsync(id);
        if (row == null) return NotFound();

        _db.OntologySynonyms.Remove(row);
        await _db.SaveChangesAsync();

        _logger.LogInformation("{User} deleted synonym '{Synonym}' of {Code}", User.Identity?.Name, row.Synonym, row.Code);
        TempData["Message"] = $"Deleted \"{row.Synonym}\" from {row.Code}.";
        return Back(q, row.Code);
    }

    // POST /Synonyms/DeleteCode — removes all synonyms of a code (the code itself stays in the datasets)
    [HttpPost("DeleteCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCode(string code, string? q)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        var rows = await _db.OntologySynonyms.Where(s => s.Code.ToUpper() == normalized).ToListAsync();
        _db.OntologySynonyms.RemoveRange(rows);
        await _db.SaveChangesAsync();

        _logger.LogInformation("{User} deleted all {Count} synonyms of {Code}", User.Identity?.Name, rows.Count, normalized);
        TempData["Message"] = $"Deleted all {rows.Count} synonyms of {normalized}.";
        return Back(q);
    }

    // ---------------------------------------------------------------------

    private IActionResult Back(string? q, string? code = null)
    {
        var url = Url.Action(nameof(Index), new { q = string.IsNullOrWhiteSpace(q) ? null : q })!;
        return Redirect(code == null ? url : url + "#code-" + Anchor(code));
    }

    public static string Anchor(string code) => Regex.Replace(code, "[^A-Za-z0-9]", "-");

    private static string? UrlFor(string code) => OntologyUrlHelper.Parse(code).Select(x => x.Url).FirstOrDefault();

    /// <summary>"uberon_0000955" / " UBERON:0000955 " → "UBERON:0000955"; null if not a code.</summary>
    public static string? NormalizeCode(string? code)
    {
        var m = Regex.Match((code ?? "").Trim(), @"^([A-Za-z][A-Za-z0-9]*)\s*[:_\-]\s*([A-Za-z]*\d[A-Za-z0-9]*)$");
        return m.Success ? $"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant() : null;
    }

    /// <summary>Splits on new lines and ';', trims, drops empty / "NULL" / duplicates.</summary>
    public static List<string> SplitSynonyms(string? raw) =>
        (raw ?? "").Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanSynonym)
            .Where(s => s != null)
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? CleanSynonym(string? s)
    {
        var t = Regex.Replace((s ?? "").Trim(), @"\s+", " ");
        if (t.Length == 0 || t.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
        return t.Length > 300 ? t[..300] : t;
    }

    // "UberonOrganOrTissue" → "Uberon Organ Or Tissue"
    private static string Pretty(string category) =>
        Regex.Replace(category ?? "", "(?<=[a-z0-9])(?=[A-Z])", " ");
}
