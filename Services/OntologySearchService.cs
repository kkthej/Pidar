using Microsoft.EntityFrameworkCore;
using Pidar.Data;

namespace Pidar.Services;

public sealed class OntologySearchService
{
    private readonly PidarDbContext _db;

    public OntologySearchService(PidarDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Resolve a free-text query into ontology codes.
    /// - If user types a code (contains ':'), include it.
    /// - Also match synonyms by substring (fast via trigram index).
    /// </summary>
    public async Task<List<string>> ResolveCodesAsync(string q, int maxCodes = 50)
    {
        q = q.Trim();
        if (q.Length == 0) return [];

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Direct code input (NCIT:..., DOID:..., UBERON:...)
        if (q.Contains(':'))
            codes.Add(q);

        // Synonym → codes
        var fromSyn = await _db.OntologySynonyms
            .AsNoTracking()
            .Where(x => EF.Functions.ILike(x.Synonym, $"%{q}%"))
            .Select(x => x.Code)
            .Distinct()
            .Take(maxCodes)
            .ToListAsync();

        foreach (var c in fromSyn)
            codes.Add(c);

        return codes.ToList();
    }
}
