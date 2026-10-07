namespace Pidar.Helpers
{
    /// <summary>
    /// Display labels that differ from the property name, used by the Create/Edit/Details/Delete views
    /// and the Excel prefill (whose label map is built from these labels).
    /// Only the label shown on screen changes: database columns and export headers stay the same.
    /// Wording follows metadata_template.xlsx (ver2) and the PIDAR schema.
    /// </summary>
    public static class FieldLabels
    {
        private static readonly Dictionary<string, string> Overrides = new(StringComparer.Ordinal)
        {
            ["DiseaseCategory"] = "Main Disease Category",
            ["DiseaseModel"] = "Specific Disease Model",
        };

        /// <param name="property">The property name, e.g. "DiseaseModel" (or "InVivo.DiseaseModel").</param>
        public static bool TryGet(string? property, out string label)
        {
            var key = property ?? "";
            var dot = key.LastIndexOf('.');
            if (dot >= 0) key = key[(dot + 1)..];
            return Overrides.TryGetValue(key, out label!);
        }
    }
}
