using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models.Ontology;

namespace Pidar.Services;

public sealed class OntologyIndexService
{
    private readonly PidarDbContext _db;

    public OntologyIndexService(PidarDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Rebuilds dataset_ontology_term rows for ONE dataset
    /// based on the current Ontology row (single + comma-separated codes).
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public async Task RebuildAsync(int datasetId)
    {
        // 1) Remove existing terms for this dataset
        var existing = _db.DatasetOntologyTerms
            .Where(t => t.DatasetId == datasetId);

        _db.DatasetOntologyTerms.RemoveRange(existing);

        // 2) Load ontology row
        var ontology = await _db.Ontologies
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.DatasetId == datasetId);

        if (ontology == null)
        {
            await _db.SaveChangesAsync();
            return;
        }

        // 3) Reflect over all string properties of Ontology
        var stringProps = typeof(Ontology)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string));

        foreach (var prop in stringProps)
        {
            var raw = (string?)prop.GetValue(ontology);
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            // Supports single code OR comma-separated codes
            var codes = raw.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

            foreach (var code in codes)
            {
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                _db.DatasetOntologyTerms.Add(new DatasetOntologyTerm
                {
                    DatasetId = datasetId,
                    Category = prop.Name, // e.g. "UberonOrganOrTissue"
                    Code = code
                });
            }
        }

        // 4) Persist
        await _db.SaveChangesAsync();
    }
}
