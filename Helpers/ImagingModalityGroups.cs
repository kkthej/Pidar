using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Pidar.Helpers
{
    /// <summary>
    /// The 12 standard Imaging Modality groups agreed by the PIDAR team (Dario Longo, 7 Oct 2026),
    /// used by the Create/Edit pick-list, the search ("Imaging Modality" field), the dataset list
    /// and the Statistics page.
    ///
    /// The value is still stored as text in StudyComponent.ImagingModality, e.g. "PET/CT, MRI".
    /// Rules for reading a value:
    ///   - split into items on "," ";" "+" " and "
    ///   - each item is normalised: full names → abbreviations ("Magnetic Resonance Imaging" → MRI,
    ///     "Optical Imaging" → OI, "photoacoustic" → OA, …), "PET-CT"/"PETCT" → PET/CT
    ///   - items that fit no group are kept as they are ("not in list")
    /// wwwroot/js/imaging-modality.js holds the same rules for the browser: keep them in sync.
    /// </summary>
    public static class ImagingModalityGroups
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "CT", "PET", "PET/CT", "SPECT", "SPECT/CT", "MRI", "PET/MRI", "US", "OI", "OA", "EPRI", "MPI"
        };

        public static readonly IReadOnlyDictionary<string, string> FullNames = new Dictionary<string, string>
        {
            ["CT"] = "Computed Tomography",
            ["PET"] = "Positron Emission Tomography",
            ["PET/CT"] = "PET combined with CT",
            ["SPECT"] = "Single Photon Emission Computed Tomography",
            ["SPECT/CT"] = "SPECT combined with CT",
            ["MRI"] = "Magnetic Resonance Imaging",
            ["PET/MRI"] = "PET combined with MRI",
            ["US"] = "Ultrasound",
            ["OI"] = "Optical Imaging",
            ["OA"] = "Optoacoustic / Photoacoustic Imaging",
            ["EPRI"] = "Electron Paramagnetic Resonance Imaging",
            ["MPI"] = "Magnetic Particle Imaging",
        };

        /// <summary>
        /// CSS class giving a group its colour (wwwroot/css/site.css, "IMAGING MODALITY COLOURS"):
        /// "PET/CT" → "mod-pet-ct". Combined modalities are drawn half in each colour.
        /// Null for text that fits no group, which stays grey.
        /// </summary>
        public static string? CssClass(string? group) =>
            group != null && FullNames.ContainsKey(group) ? "mod-" + group.ToLowerInvariant().Replace('/', '-') : null;

        public const string Other = "Other";
        public const string FieldName = "StudyComponent.ImagingModality";

        // Full names and variants → standard abbreviation (applied before grouping, case-insensitive)
        private static readonly (Regex Pattern, string Abbrev)[] Synonyms =
        {
            (new Regex(@"\bmagnetic\s+resonance(\s+imaging)?\b|\bnmr\b|\bmri\b|\bmr\b", RegexOptions.IgnoreCase), "MRI"),
            (new Regex(@"\bpositron\s+emission\s+tomography\b|\bpet\b", RegexOptions.IgnoreCase), "PET"),
            (new Regex(@"\bsingle[\s-]+photon\s+emission(\s+computed)?\s+tomography\b|\bspect\b", RegexOptions.IgnoreCase), "SPECT"),
            (new Regex(@"\b(micro[\s-]?)?(computed\s+tomography|ct)\b", RegexOptions.IgnoreCase), "CT"),
            (new Regex(@"\bultrasound\b|\bultrasonography\b|\bus\b", RegexOptions.IgnoreCase), "US"),
            (new Regex(@"\b(photo|opto)[\s-]?acoustic(s)?(\s+imaging)?\b|\boa\b|\bpai\b", RegexOptions.IgnoreCase), "OA"),
            (new Regex(@"\boptical\s+imaging\b|\bfluorescen\w*(\s+imaging)?\b|\bbioluminescen\w*(\s+imaging)?\b|\boi\b|\bfli\b|\bbli\b", RegexOptions.IgnoreCase), "OI"),
            (new Regex(@"\belectron\s+paramagnetic\s+resonance(\s+imaging)?\b|\bepr(i)?\b", RegexOptions.IgnoreCase), "EPRI"),
            (new Regex(@"\bmagnetic\s+particle\s+imaging\b|\bmpi\b", RegexOptions.IgnoreCase), "MPI"),
        };

        /// <summary>One item of a modality value: its group, or Group = null when it fits none.</summary>
        public sealed record Item(string Text, string? Group);

        /// <summary>"PET/CT, Magnetic Resonance Imaging, X-ray" → (PET/CT), (MRI), ("X-ray", not in list)</summary>
        public static IReadOnlyList<Item> Parse(string? modality)
        {
            var result = new List<Item>();
            if (string.IsNullOrWhiteSpace(modality)) return result;

            var items = Regex.Split(modality, @"\s*(?:,|;|\+|\band\b)\s*", RegexOptions.IgnoreCase)
                             .Select(i => Regex.Replace(i, @"\s+", " ").Trim().TrimEnd('.'))
                             .Where(i => i.Length > 0);

            foreach (var item in items)
            {
                // hybrids are written "PET/CT", "PET-CT", "PETCT", "PET CT"
                var abbrevs = Normalise(item);

                if (abbrevs.Count == 2 && abbrevs.Contains("PET") && abbrevs.Contains("CT")) result.Add(new(item, "PET/CT"));
                else if (abbrevs.Count == 2 && abbrevs.Contains("SPECT") && abbrevs.Contains("CT")) result.Add(new(item, "SPECT/CT"));
                else if (abbrevs.Count == 2 && abbrevs.Contains("PET") && abbrevs.Contains("MRI")) result.Add(new(item, "PET/MRI"));
                else if (abbrevs.Count == 0) result.Add(new(item, null));
                else foreach (var a in abbrevs) result.Add(new(item, a));   // one, or e.g. "MRI/OI" written as one item
            }
            return result;
        }

        /// <summary>The groups one dataset belongs to, in the standard order ("Other" last if something fits no group).</summary>
        public static IReadOnlyList<string> Classify(string? modality)
        {
            var items = Parse(modality);
            var groups = items.Where(i => i.Group != null).Select(i => i.Group!).ToHashSet();
            var list = All.Where(groups.Contains).ToList();
            if (items.Any(i => i.Group == null)) list.Add(Other);
            return list;
        }

        /// <summary>Items that fit no group, as written (shown as "not in list").</summary>
        public static IReadOnlyList<string> Unmatched(string? modality) =>
            Parse(modality).Where(i => i.Group == null).Select(i => i.Text)
                           .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>
        /// The value as it is saved: groups in the standard order, then the items that fit no group.
        /// "Magnetic Resonance Imaging, pet-ct" → "PET/CT, MRI". Empty → null.
        /// </summary>
        public static string? ToStoredValue(string? modality)
        {
            var items = Parse(modality);
            if (items.Count == 0) return null;
            var groups = items.Where(i => i.Group != null).Select(i => i.Group!).ToHashSet();
            var parts = All.Where(groups.Contains).Concat(Unmatched(modality));
            var value = string.Join(", ", parts);
            return value.Length == 0 ? null : value;
        }

        /// <summary>
        /// The groups that satisfy a searched group: "PET" also matches PET/CT and PET/MRI;
        /// a hybrid ("PET/CT") matches only itself.
        /// </summary>
        public static IReadOnlyList<string> Expand(string group) =>
            group.Contains('/')
                ? new[] { group }
                : All.Where(g => g.Split('/').Contains(group)).ToList();

        /// <summary>
        /// The groups a search phrase stands for, when every part of it is a modality
        /// ("MRI", "magnetic resonance imaging", "PET/CT, MRI"); otherwise empty.
        /// </summary>
        public static IReadOnlyList<string> GroupsInPhrase(string? phrase)
        {
            var items = Parse(phrase);
            if (items.Count == 0 || items.Any(i => i.Group == null)) return Array.Empty<string>();
            return items.Select(i => i.Group!).Distinct().ToList();
        }

        /// <summary>True when a dataset's modality value satisfies every searched group.</summary>
        public static bool Matches(string? modality, IReadOnlyList<string> searchedGroups)
        {
            if (searchedGroups.Count == 0) return false;
            var groups = Classify(modality);
            return searchedGroups.All(s => Expand(s).Any(groups.Contains));
        }

        /// <summary>
        /// The pick-list for Create/Edit: one toggle per group (several can be on), plus the items
        /// of the current value that fit no group, kept as "not in list".
        /// A hidden input carries the joined value; wwwroot/js/imaging-modality.js keeps it in sync.
        /// </summary>
        public static IHtmlContent RenderPicker(string? value)
        {
            static string E(string? x) => System.Net.WebUtility.HtmlEncode(x ?? "");
            var selected = Classify(value).ToHashSet();
            var unmatched = Unmatched(value);
            var stored = ToStoredValue(value) ?? "";

            var sb = new StringBuilder();
            sb.Append("<div class=\"modality-picker\" id=\"imaging-modality-picker\">");
            sb.Append($"<input type=\"hidden\" name=\"{FieldName}\" id=\"imaging-modality\" value=\"{E(stored)}\" />");
            sb.Append("<div class=\"d-flex flex-wrap gap-2\" role=\"group\" aria-label=\"Imaging modality groups\">");
            var n = 0;
            foreach (var g in All)
            {
                var id = $"modality-opt-{n++}";
                var on = selected.Contains(g) ? " checked" : "";
                sb.Append($"<input type=\"checkbox\" class=\"btn-check modality-opt\" id=\"{id}\" value=\"{E(g)}\" autocomplete=\"off\"{on}>");
                sb.Append($"<label class=\"btn btn-sm btn-outline-primary {CssClass(g)}\" for=\"{id}\" title=\"{E(FullNames[g])}\">{E(g)}</label>");
            }
            foreach (var u in unmatched)
            {
                var id = $"modality-opt-{n++}";
                sb.Append($"<input type=\"checkbox\" class=\"btn-check modality-opt modality-extra\" id=\"{id}\" value=\"{E(u)}\" autocomplete=\"off\" checked>");
                sb.Append($"<label class=\"btn btn-sm btn-outline-secondary\" for=\"{id}\" title=\"Not one of the 12 groups: untick to remove\">{E(u)} (not in list)</label>");
            }
            sb.Append("</div>");
            sb.Append("<div class=\"form-text\">Tick every modality used. Hover a button for the full name. ");
            sb.Append("Use PET/CT, SPECT/CT or PET/MRI for combined (hybrid) scanners. Details go in Imaging Sub Modality.</div>");
            if (!string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), stored, StringComparison.Ordinal))
                sb.Append($"<div class=\"form-text text-muted\">Previously written as: <em>{E(value.Trim())}</em></div>");
            sb.Append("</div>");
            return new HtmlString(sb.ToString());
        }

        // Abbreviations found in one item, e.g. "PET/CT" → {PET, CT}; "Magnetic Resonance Imaging" → {MRI}
        private static HashSet<string> Normalise(string item)
        {
            var found = new HashSet<string>();
            var rest = item.Replace("PETCT", "PET CT", StringComparison.OrdinalIgnoreCase)
                           .Replace("SPECTCT", "SPECT CT", StringComparison.OrdinalIgnoreCase);
            foreach (var (pattern, abbrev) in Synonyms)
            {
                if (pattern.IsMatch(rest))
                {
                    found.Add(abbrev);
                    rest = pattern.Replace(rest, " ");   // so "SPECT" isn't also read as "CT"
                }
            }
            return found;
        }
    }
}
