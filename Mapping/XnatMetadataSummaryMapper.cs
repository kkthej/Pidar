using System.Text.Json;
using Pidar.Models.Summaries;

namespace Pidar.Mapping;

public static class XnatMetadataSummaryMapper
{
    public static DatasetSummary FromMetadataJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new DatasetSummary();

        using var doc = JsonDocument.Parse(json);

        JsonElement obj;
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            obj = doc.RootElement[0];
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            obj = doc.RootElement;
        else
            return new DatasetSummary();

        string? GetString(string key)
        {
            if (!obj.TryGetProperty(key, out var el)) return null;

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        int? GetInt(string key)
        {
            if (!obj.TryGetProperty(key, out var el)) return null;

            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
                return n;

            if (el.ValueKind == JsonValueKind.String &&
                int.TryParse(el.GetString(), out var s))
                return s;

            return null;
        }

        return new DatasetSummary
        {
            DisplayId = GetInt("Dataset: Dataset ID") ?? 0,
            Species = GetString("In Vivo: Species"),
            OrganOrTissue = GetString("In Vivo: Organ/Tissue"),
            DiseaseModel = GetString("In Vivo: Disease model"),
            SampleSize = GetString("In Vivo: Sample size"),
            ImagingModality = GetString("Study Component: Imaging modality")
        };
    }
}