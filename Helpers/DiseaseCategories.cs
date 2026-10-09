namespace Pidar.Helpers
{
    /// <summary>
    /// Options for the "Disease Category" dropdown (InVivo.DiseaseCategory) and the ontology code
    /// that is filled into Ontology.DoidDiseaseCategory when a category is chosen.
    /// Eight main classes of the Disease Ontology (https://disease-ontology.org), plus "Healthy",
    /// which is not a disease and so has no DOID: it uses an NCIT code instead.
    /// Edit this list to add/rename categories; existing values that are not in the list are kept.
    /// </summary>
    public static class DiseaseCategories
    {
        /// <param name="Code">Ontology code stored with the category: a DOID, or NCIT for Healthy.</param>
        public sealed record Option(string Label, string? Code);

        /// <summary>
        /// Code for healthy animals (Dario Longo, 9 Oct 2026: NCIT or OBI, not DOID).
        /// NCIT:C115935 "Healthy Subject" fits animals that are healthy whether or not they are a control group.
        /// Alternatives offered: NCIT:C94342 (Healthy Control), OBI:0001325 (healthy control role).
        /// To be confirmed by the ontology working group; changing it here changes it everywhere,
        /// but datasets already saved keep the old code until they are edited (or updated with SQL).
        /// </summary>
        public const string HealthyCode = "NCIT:C115935";
        public const string HealthyLabel = "Healthy";

        // The eight main categories agreed by the PIDAR team (Dario Longo, 7 Oct 2026),
        // matching the "disease category" row of metadata_template.xlsx (ver2), plus Healthy (9 Oct 2026).
        // Healthy is 9th so that the numbers 1-8 used in already-filled templates keep their meaning.
        // DOID codes to be confirmed by the ontology working group.
        public static readonly IReadOnlyList<Option> All = new List<Option>
        {
            new("Disease of cellular proliferation", "DOID:14566"),
            new("Disease of anatomical entity",      "DOID:7"),
            new("Disease by infectious agent",       "DOID:0050117"),
            new("Genetic disease",                   "DOID:630"),
            new("Disease of metabolism",             "DOID:0014667"),
            new("Disease of mental health",          "DOID:150"),
            new("Physical disorder",                 "DOID:0060035"),
            new("Syndrome",                          "DOID:225"),
            new(HealthyLabel,                        HealthyCode),
        };

        private static readonly HashSet<string> HealthyAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "healthy animals", "healthy animal", "healthy subject", "healthy subjects",
            "healthy control", "healthy controls", "control", "controls", "no disease", "none (healthy)",
        };

        public const string FieldName = "InVivo.DiseaseCategory";
        public const string OntologyFieldName = "Ontology.DoidDiseaseCategory";

        /// <summary>
        /// The dropdown for Create/Edit. Each option carries its code in data-doid;
        /// wwwroot/js/disease-category.js copies it into the Doid Disease Category field.
        /// A stored value that is not in the list (older data) is kept as an extra option.
        /// </summary>
        public static Microsoft.AspNetCore.Html.IHtmlContent RenderSelect(string? value)
        {
            static string E(string? x) => System.Net.WebUtility.HtmlEncode(x ?? "");
            var current = value?.Trim() ?? "";
            var match = Find(current);
            var sb = new System.Text.StringBuilder();
            sb.Append($"<select class=\"form-select\" name=\"{FieldName}\" id=\"disease-category\" data-doid-target=\"{OntologyFieldName}\">");
            sb.Append("<option value=\"\">— choose a category —</option>");
            foreach (var o in All)
            {
                var selected = match != null && match.Label == o.Label ? " selected" : "";
                sb.Append($"<option value=\"{E(o.Label)}\" data-doid=\"{E(o.Code)}\"{selected}>{E(o.Label)}{(o.Code != null ? $" ({E(o.Code)})" : "")}</option>");
            }
            if (match == null && current.Length > 0)
                sb.Append($"<option value=\"{E(current)}\" data-doid=\"\" selected>{E(current)} (not in list)</option>");
            sb.Append("</select>");
            return new Microsoft.AspNetCore.Html.HtmlString(sb.ToString());
        }

        /// <summary>
        /// Case-insensitive lookup by label, by its number in the template ("5", "5. Disease of metabolism"),
        /// or a common wording for healthy animals ("healthy animals", "healthy subject", "control").
        /// </summary>
        public static Option? Find(string? label)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;
            var t = label.Trim();
            // "5" or "5. Disease of metabolism" (numbering used in the template instructions)
            var m = System.Text.RegularExpressions.Regex.Match(t, @"^(\d+)\s*[.)]?\s*(.*)$");
            if (m.Success)
            {
                if (m.Groups[2].Value.Length == 0 && int.TryParse(m.Groups[1].Value, out var n) && n >= 1 && n <= All.Count)
                    return All[n - 1];
                if (m.Groups[2].Value.Length > 0) t = m.Groups[2].Value.Trim();
            }
            var hit = All.FirstOrDefault(o => o.Label.Equals(t, StringComparison.OrdinalIgnoreCase));
            if (hit != null) return hit;
            return HealthyAliases.Contains(t) ? All.First(o => o.Label == HealthyLabel) : null;
        }
    }
}
