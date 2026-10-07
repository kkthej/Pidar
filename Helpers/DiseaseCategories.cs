namespace Pidar.Helpers
{
    /// <summary>
    /// Options for the "Disease Category" dropdown (InVivo.DiseaseCategory) and the DOID code
    /// that is filled into Ontology.DoidDiseaseCategory when a category is chosen.
    /// Top-level classes of the Disease Ontology (https://disease-ontology.org).
    /// Edit this list to add/rename categories; existing values that are not in the list are kept.
    /// </summary>
    public static class DiseaseCategories
    {
        public sealed record Option(string Label, string? Doid);

        public static readonly IReadOnlyList<Option> All = new List<Option>
        {
            new("Cancer",                   "DOID:162"),
            new("Cardiovascular disease",   "DOID:1287"),
            new("Nervous system disease",   "DOID:863"),
            new("Metabolic disease",        "DOID:0014667"),
            new("Infectious disease",       "DOID:0050117"),
            new("Immune system disease",    "DOID:2914"),
            new("Musculoskeletal disease",  "DOID:17"),
            new("Respiratory disease",      "DOID:1579"),
            new("Gastrointestinal disease", "DOID:77"),
            new("Urinary system disease",   "DOID:18"),
            new("Genetic disease",          "DOID:630"),
            new("Healthy / no disease",     null),
            new("Other",                    null),
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
        public static Option? Find(string? label) =>
            string.IsNullOrWhiteSpace(label)
                ? null
                : All.FirstOrDefault(o => o.Label.Equals(label.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
