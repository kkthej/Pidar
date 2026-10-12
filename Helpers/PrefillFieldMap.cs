using System.Text.Json;

namespace Pidar.Helpers
{
    /// <summary>
    /// The label → field map read by wwwroot/js/metadata-prefill.js (data-field-map attribute):
    /// "paper title" → "Publication.PaperTitle", built from CategoryProvider so adding a field there keeps
    /// the Excel prefill in sync. Used by the Create page and by the contribution form.
    /// </summary>
    public static class PrefillFieldMap
    {
        // The only xlsx export labels that differ from PrettyLabel(propertyName).
        // Verified by comparing every CategoryProvider field against the actual xlsx dump.
        // Do NOT add entries here unless PrettyLabel of the C# property genuinely differs
        // from the xlsx column A label — the auto-generated map handles everything else.
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            // Publication — export uses short display names (strips "Paper" prefix)
            ["authors"] = "Publication.PaperAuthors",
            ["journal"] = "Publication.PaperJournal",
            ["year"] = "Publication.PaperYear",
            ["doi"] = "Publication.PaperDoi",

            // InVivo — export uses short display names from DownloadController.PrettyLabelMap
            ["sample size"] = "InVivo.OverallSampleSize",
            ["organ/tissue"] = "InVivo.OrganOrTissue",
        };

        public static Dictionary<string, string> Build(Dictionary<string, List<string>> categories)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categories)
            {
                foreach (var field in cat.Value)
                {
                    var propName = field.Split('.')[1];
                    map[PrettyLabel(propName).ToLowerInvariant()] = field;
                }
            }
            foreach (var alias in Aliases)
                map[alias.Key] = alias.Value;
            return map;
        }

        public static string BuildJson(Dictionary<string, List<string>> categories) =>
            JsonSerializer.Serialize(Build(categories));

        /// <summary>Same label rule as the Create page: custom label, else split camel case.</summary>
        public static string PrettyLabel(string raw)
        {
            if (FieldLabels.TryGet(raw, out var customLabel)) return customLabel;
            raw = raw.Replace("/", " ");

            var formatted = System.Text.RegularExpressions.Regex.Replace(
                raw,
                @"(?<=[a-z0-9])(?=[A-Z][a-z])",
                " ");

            return char.ToUpper(formatted[0]) + formatted.Substring(1);
        }
    }
}
