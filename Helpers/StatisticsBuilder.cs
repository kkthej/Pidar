using System.Text.RegularExpressions;

namespace Pidar.Helpers
{
    /// <summary>
    /// Builds the numbers for the Statistics page from one small row per dataset.
    /// Free-text fields (sex, access, status, organ, species) are tidied here only for counting;
    /// the stored data is never changed.
    /// </summary>
    public static class StatisticsBuilder
    {
        /// <summary>The fields the page needs, one per dataset (loaded in one query).</summary>
        public sealed record Row(
            string? ImagingModality, string? DiseaseCategory, string? OrganOrTissue, string? Species,
            string? SampleSize, string? Sex, string? Country, string? Access, string? Status,
            string? UpdatedYear, string? PaperLinked);

        /// <summary>One bar: label, count, optional hint (tooltip / small grey text).</summary>
        public sealed record Bar(string Label, int Count, string? Hint = null);

        public sealed record Result(
            int Datasets, int Animals, int Countries, int ModalitiesUsed, int ModalitiesTotal,
            int CategoriesUsed, int CategoriesTotal, int WithPublication, int WithoutCategory,
            IReadOnlyList<Bar> Modalities, IReadOnlyList<Bar> Categories, IReadOnlyList<Bar> Organs,
            IReadOnlyList<Bar> CountryBars, IReadOnlyList<Bar> Species, IReadOnlyList<Bar> Sex,
            IReadOnlyList<Bar> Access, IReadOnlyList<Bar> Status, IReadOnlyList<Bar> Years);

        public const string NotStated = "Not stated";

        public static Result Build(IReadOnlyList<Row> rows)
        {
            // ---- imaging modality: all 12 groups, zero included (a dataset counts once per group) ----
            var modCounts = Count(rows.SelectMany(r => ImagingModalityGroups.Classify(r.ImagingModality).Distinct()));
            var modalities = ImagingModalityGroups.All
                .Select(g => new Bar(g, modCounts.GetValueOrDefault(g), ImagingModalityGroups.FullNames[g]))
                .ToList();
            if (modCounts.TryGetValue(ImagingModalityGroups.Other, out var otherMods))
                modalities.Add(new Bar("Other", otherMods, "Not one of the 12 groups"));

            // ---- main disease category: all 8, zero included, plus older values not in the list ----
            var catCounts = Count(rows.Where(r => !string.IsNullOrWhiteSpace(r.DiseaseCategory))
                                      .Select(r => DiseaseCategories.Find(r.DiseaseCategory)?.Label ?? r.DiseaseCategory!.Trim()));
            var categories = DiseaseCategories.All
                .Select(o => new Bar(o.Label, catCounts.GetValueOrDefault(o.Label), o.Doid))
                .Concat(catCounts.Where(kv => DiseaseCategories.Find(kv.Key) == null)
                                 .Select(kv => new Bar(kv.Key, kv.Value, "Not in the list")))
                .ToList();

            // ---- organs (top 10), countries, species ----
            var organs = Count(rows.SelectMany(r => Organs(r.OrganOrTissue)))
                .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Take(10).Select(kv => new Bar(kv.Key, kv.Value)).ToList();

            var countryCounts = Count(rows.Select(r => Clean(r.Country)).Where(c => c != null)!);
            var countries = countryCounts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Select(kv => new Bar(kv.Key, kv.Value)).ToList();

            var animalsBySpecies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var speciesCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var sp = SpeciesName(r.Species);
                speciesCounts[sp] = speciesCounts.GetValueOrDefault(sp) + 1;
                animalsBySpecies[sp] = animalsBySpecies.GetValueOrDefault(sp) + ParseInt(r.SampleSize);
            }
            var species = speciesCounts.OrderByDescending(kv => kv.Value)
                .Select(kv => new Bar(kv.Key, kv.Value, $"{animalsBySpecies[kv.Key]} animals")).ToList();

            // ---- study design & sharing (fixed segment order, "Not stated" last) ----
            var sex = Segments(rows.Select(r => SexGroup(r.Sex)), "Female", "Male", "Both");
            var access = Segments(rows.Select(r => AccessGroup(r.Access)), "Public", "Limited");
            var status = Segments(rows.Select(r => StatusGroup(r.Status)), "Complete", "Ongoing");

            // ---- datasets per year (year of last update) ----
            var years = Count(rows.Select(r => Clean(r.UpdatedYear)).Where(y => y != null && Regex.IsMatch(y, @"^\d{4}$"))!)
                .OrderBy(kv => kv.Key).Select(kv => new Bar(kv.Key, kv.Value)).ToList();

            return new Result(
                Datasets: rows.Count,
                Animals: rows.Sum(r => ParseInt(r.SampleSize)),
                Countries: countryCounts.Count,
                ModalitiesUsed: ImagingModalityGroups.All.Count(g => modCounts.GetValueOrDefault(g) > 0),
                ModalitiesTotal: ImagingModalityGroups.All.Count,
                CategoriesUsed: DiseaseCategories.All.Count(o => catCounts.GetValueOrDefault(o.Label) > 0),
                CategoriesTotal: DiseaseCategories.All.Count,
                WithPublication: rows.Count(r => HasPublication(r.PaperLinked)),
                WithoutCategory: rows.Count(r => string.IsNullOrWhiteSpace(r.DiseaseCategory)),
                Modalities: modalities, Categories: categories, Organs: organs, CountryBars: countries,
                Species: species, Sex: sex, Access: access, Status: status, Years: years);
        }

        // ------------------------------------------------------------------ tidy-up rules

        // "brain, adipose tissue" / "brain and liver" / "epidermis (melanoma)" → separate organs, one count per dataset
        private static readonly Dictionary<string, string> OrganAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["kidneys"] = "kidney",
            ["epidermis"] = "skin / epidermis", ["skin"] = "skin / epidermis", ["melanoma"] = "skin / epidermis",
            ["hepatocellular carcinoma"] = "liver",
            ["skeletal muscles"] = "skeletal muscle",
            ["total body"] = "whole body",
            ["tumor"] = "tumour",
        };

        public static IEnumerable<string> Organs(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Enumerable.Empty<string>();
            return Regex.Split(value, @"\s*(?:,|;|\(|\)|\band\b)\s*", RegexOptions.IgnoreCase)
                .Select(o => Regex.Replace(o, @"\s+", " ").Trim().TrimEnd('.').ToLowerInvariant())
                .Where(o => o.Length > 1)
                .Select(o => OrganAliases.TryGetValue(o, out var a) ? a : o)
                .Select(Capitalise)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public static string SexGroup(string? v)
        {
            var s = (v ?? "").ToLowerInvariant();
            var female = s.Contains("female");
            var male = Regex.IsMatch(s.Replace("female", ""), @"\bmales?\b");
            return female && male ? "Both" : female ? "Female" : male ? "Male" : NotStated;
        }

        public static string AccessGroup(string? v)
        {
            var s = (v ?? "").Trim().ToLowerInvariant();
            if (s.Length == 0) return NotStated;
            if (s.Contains("public") || s.Contains("open")) return "Public";
            if (s.Contains("limit") || s.Contains("restrict") || s.Contains("request")) return "Limited";
            return "Other";
        }

        public static string StatusGroup(string? v)
        {
            var s = (v ?? "").Trim().ToLowerInvariant();
            if (s.Length == 0) return NotStated;
            if (s.StartsWith("complet")) return "Complete";
            if (s.StartsWith("ongoing") || s.Contains("progress")) return "Ongoing";
            return "Other";
        }

        public static bool HasPublication(string? v)
        {
            var s = (v ?? "").Trim().ToLowerInvariant();
            return s.StartsWith("yes") || s.StartsWith("http") || s.StartsWith("doi");
        }

        private static string SpeciesName(string? v) => Clean(v) is { } s ? Capitalise(s.ToLowerInvariant()) : NotStated;

        private static List<Bar> Segments(IEnumerable<string> values, params string[] order)
        {
            var c = Count(values);
            var list = order.Select(k => new Bar(k, c.GetValueOrDefault(k))).ToList();
            if (c.TryGetValue("Other", out var other)) list.Add(new Bar("Other", other));
            list.Add(new Bar(NotStated, c.GetValueOrDefault(NotStated)));
            return list.Where(b => b.Count > 0 || order.Contains(b.Label)).ToList();
        }

        private static Dictionary<string, int> Count(IEnumerable<string> values) =>
            values.GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                  .ToDictionary(g => g.First(), g => g.Count(), StringComparer.OrdinalIgnoreCase);

        private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : Regex.Replace(v, @"\s+", " ").Trim();

        private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

        private static int ParseInt(string? v) =>
            int.TryParse((v ?? "").Replace(",", "").Trim(), out var n) && n > 0 ? n : 0;
    }
}
