using System.Text.RegularExpressions;

namespace Pidar.Helpers
{
    /// <summary>
    /// Classifies the free-text Imaging Modality of a dataset into the 12 standard groups agreed by the
    /// PIDAR team (Dario Longo, 7 Oct 2026). Used by the Statistics page.
    ///
    /// Rules:
    ///   - the value is split into items on "," ";" " and " "+" (e.g. "PET/CT, MRI" → "PET/CT" and "MRI")
    ///   - each item is normalised: full names → abbreviations ("Magnetic Resonance Imaging" → MRI,
    ///     "Optical Imaging" → OI, "photoacoustic" → OA, …), "PET-CT"/"PETCT" → PET/CT
    ///   - a dataset counts once in every group it contains; items that fit no group count as "Other"
    /// </summary>
    public static class ImagingModalityGroups
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "CT", "PET", "PET/CT", "SPECT", "SPECT/CT", "MRI", "PET/MRI", "US", "OI", "OA", "EPRI", "MPI"
        };

        public const string Other = "Other";

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

        /// <summary>The groups one dataset belongs to, in the standard order (Other last).</summary>
        public static IReadOnlyList<string> Classify(string? modality)
        {
            if (string.IsNullOrWhiteSpace(modality)) return Array.Empty<string>();

            var groups = new HashSet<string>();
            var items = Regex.Split(modality, @"\s*(?:,|;|\+|\band\b)\s*", RegexOptions.IgnoreCase)
                             .Where(i => !string.IsNullOrWhiteSpace(i));

            foreach (var item in items)
            {
                // hybrids are written "PET/CT", "PET-CT", "PETCT", "PET CT"
                var abbrevs = Normalise(item);

                string? group = null;
                if (abbrevs.Count == 2 && abbrevs.Contains("PET") && abbrevs.Contains("CT")) group = "PET/CT";
                else if (abbrevs.Count == 2 && abbrevs.Contains("SPECT") && abbrevs.Contains("CT")) group = "SPECT/CT";
                else if (abbrevs.Count == 2 && abbrevs.Contains("PET") && abbrevs.Contains("MRI")) group = "PET/MRI";
                else if (abbrevs.Count == 1) group = abbrevs.First();
                else if (abbrevs.Count > 1)
                {
                    foreach (var a in abbrevs) groups.Add(a);   // e.g. "MRI/OI" written as one item
                    continue;
                }

                groups.Add(group ?? Other);
            }

            return All.Where(groups.Contains)
                      .Concat(groups.Contains(Other) ? new[] { Other } : Array.Empty<string>())
                      .ToList();
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
