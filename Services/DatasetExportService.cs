using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Exports;
using Pidar.Models;
using System.Text;
using System.Text.Json;

namespace Pidar.Services;

public sealed class DatasetExportService : IDatasetExportService
{
    private readonly PidarDbContext _db;

    // Keep section labels stable (same concept as DownloadController)
    private static readonly Dictionary<string, Func<Dataset, object?>> Sections = new()
    {
        { "Dataset", ds => ds },
        { "Study Design", ds => ds.StudyDesign },
        { "Publication", ds => ds.Publication },
        { "Study Component", ds => ds.StudyComponent },
        { "Dataset Info", ds => ds.DatasetInfo },
        { "In Vivo", ds => ds.InVivo },
        { "Procedures", ds => ds.Procedures },
        { "Image Acquisition", ds => ds.ImageAcquisition },
        { "Image Data", ds => ds.ImageData },
        { "Image Correlation", ds => ds.ImageCorrelation },
        { "Analyzed", ds => ds.Analyzed },
        { "Ontology", ds => ds.Ontology }
    };

    public DatasetExportService(PidarDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportDatasetJsonAsync(int datasetId)
    {
        var ds = await LoadDatasetAsync(datasetId);

        // XNAT expects an array, even if empty
        if (ds == null)
        {
            return Encoding.UTF8.GetBytes("[]");
        }

        var flat = Flatten(ds);

        var json = JsonSerializer.Serialize(
            new[] { flat }, // 🔑 XNAT requires array-of-objects
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        return Encoding.UTF8.GetBytes(json);
    }


    public async Task<byte[]> ExportDatasetCsvAsync(int datasetId)
    {
        var ds = await LoadDatasetAsync(datasetId);
        if (ds == null) return Encoding.UTF8.GetBytes("");

        var flat = Flatten(ds);
        if (flat.Count == 0) return Encoding.UTF8.GetBytes("");

        var headers = flat.Keys.ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        sb.AppendLine(string.Join(",", headers.Select(h =>
        {
            var val = flat[h]?.ToString() ?? "";
            val = val.Replace("\r", " ").Replace("\n", " ");
            return EscapeCsv(val);
        })));

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<Dataset?> LoadDatasetAsync(int datasetId)
    {
        return await _db.Datasets
            .AsNoTracking()
            .Include(d => d.StudyDesign)
            .Include(d => d.Publication)
            .Include(d => d.StudyComponent)
            .Include(d => d.DatasetInfo)
            .Include(d => d.InVivo)
            .Include(d => d.Procedures)
            .Include(d => d.ImageAcquisition)
            .Include(d => d.ImageData)
            .Include(d => d.ImageCorrelation)
            .Include(d => d.Analyzed)
            .Include(d => d.Ontology)
            .FirstOrDefaultAsync(d => d.DatasetId == datasetId);
    }

    private static Dictionary<string, object?> Flatten(Dataset ds)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var sec in Sections)
        {
            var obj = sec.Value(ds);
            if (obj == null) continue;

            foreach (var p in obj.GetType().GetProperties())
            {
                if (SkipProp(p.Name)) continue;
                if (!IsSimpleType(p.PropertyType)) continue;

                var val = p.GetValue(obj);
                if (val == null) continue;

                if (val is string s && string.IsNullOrWhiteSpace(s)) continue;

                var label = ExportLabelMap.Map.TryGetValue(p.Name, out var mapped)
                    ? mapped
                    : Pretty(p.Name);

                dict[$"{sec.Key}: {label}"] = val;

            }
        }

        return dict;
    }

    private static bool SkipProp(string p)
    {
        if (p == "DisplayId") return false;
        if (p == "DatasetId") return true;
        if (p.EndsWith("Id")) return true;
        if (p == "Dataset") return true; // avoid navigation
        return false;
    }

    private static bool IsSimpleType(Type t)
    {
        return t.IsPrimitive ||
               t.IsEnum ||
               t == typeof(string) ||
               t == typeof(decimal) ||
               t == typeof(DateTime) ||
               t == typeof(Guid) ||
               t == typeof(bool) ||
               t == typeof(double) ||
               t == typeof(float) ||
               t == typeof(int) ||
               t == typeof(long);
    }

    private static string Pretty(string name)
        => System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

    private static string EscapeCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
