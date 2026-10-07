namespace Pidar.Helpers
{
    /// <summary>
    /// Options for the "Disease Category" dropdown (InVivo.DiseaseCategory) and the DOID code
    /// that is filled into Ontology.DoidDiseaseCategory when a category is chosen.
    /// Main classes of the Disease Ontology (https://disease-ontology.org).
    /// Healthy animals: leave the category empty (see "Animal Condition").
    /// Edit this list to add/rename categories; existing values that are not in the list are kept.
    /// </summary>
    public static class DiseaseCategories
    {
        public sealed record Option(string Label, string? Doid);

        // The eight main categories agreed by the PIDAR team (Dario Longo, 7 Oct 2026),
        // matching the "disease category" row of metadata_template.xlsx (ver2).
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
        };

        public const string FieldName = "InVivo.DiseaseCategory";
        public const string OntologyFieldName = "Ontology.DoidDiseaseCategory";

        /// <summary>
        /// The dropdown for Create/Edit. Each option carries its DOID in data-doid;
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
                sb.Append($"<option value=\"{E(o.Label)}\" data-doid=\"{E(o.Doid)}\"{selected}>{E(o.Label)}{(o.Doid != null ? $" ({E(o.Doid)})" : "")}</option>");
            }
            if (match == null && current.Length > 0)
                sb.Append($"<option value=\"{E(current)}\" data-doid=\"\" selected>{E(current)} (not in list)</option>");
            sb.Append("</select>");
            return new Microsoft.AspNetCore.Html.HtmlString(sb.ToString());
        }

        /// <summary>Case-insensitive lookup by label ("cancer" → Cancer).</summary>
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
            return All.FirstOrDefault(o => o.Label.Equals(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}
