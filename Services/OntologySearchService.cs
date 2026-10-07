using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;

namespace Pidar.Services;

/// <summary>
/// Dataset search.
///
/// How a search phrase is matched (e.g. "breast cancer mice"):
///   1. The phrase is split into words; EVERY word must match (AND).
///   2. A word matches a dataset if it appears in ANY text field of the dataset
///      OR it maps (via ontology synonyms or a typed code) to an ontology code the dataset has.
///   3. Words are matched at word boundaries, not as raw substrings:
///        - words of 1–3 characters must match a whole word ("CT" matches "PET/CT", not "structures")
///        - longer words match the start of a word ("isoflur" matches "isoflurane")
///   4. Typed ontology codes are case-insensitive ("ncit:c16809" = "NCIT:C16809").
/// </summary>
public sealed class OntologySearchService
{
    private readonly PidarDbContext _db;

    public OntologySearchService(PidarDbContext db)
    {
        _db = db;
    }

    private const int MaxTerms = 8;

    /// <summary>Splits a phrase into distinct search words (max 8).</summary>
    public static List<string> Tokenize(string phrase) =>
        phrase.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(t => t.Trim(',', ';', '"', '\''))
              .Where(t => t.Length > 0)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .Take(MaxTerms)
              .ToList();

    /// <summary>
    /// PostgreSQL regex (used with ~*, case-insensitive) that matches the term at a word boundary.
    /// Word boundaries are "start/end of text or a non-alphanumeric character", so terms that
    /// start with symbols ("[18F]FDG", "Balb/c") still work.
    /// </summary>
    public static string WordPattern(string term)
    {
        var escaped = Regex.Replace(term, @"[\\.^$*+?()\[\]{}|]", @"\$0");
        var start = "(^|[^[:alnum:]])";
        var end = term.Length <= 3 ? "([^[:alnum:]]|$)" : "";
        return start + escaped + end;
    }

    /// <summary>Ontology codes for one search word: synonyms matching at word boundaries, plus a typed code.</summary>
    public async Task<List<string>> ResolveCodesForTermAsync(string term, int maxCodes = 50)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Typed code: NCIT:C16809, uberon_0000955, CLO-0001794 → canonical upper-case PREFIX:ID
        var m = Regex.Match(term, @"^([A-Za-z][A-Za-z0-9]*)[:_\-]([A-Za-z]*\d[A-Za-z0-9]*)$");
        if (m.Success)
            codes.Add($"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant());

        var pattern = WordPattern(term);
        var fromSyn = await _db.OntologySynonyms
            .AsNoTracking()
            .Where(x => Regex.IsMatch(x.Synonym, pattern, RegexOptions.IgnoreCase))
            .Select(x => x.Code)
            .Distinct()
            .Take(maxCodes)
            .ToListAsync();

        foreach (var c in fromSyn)
            codes.Add(c.Trim().ToUpperInvariant());

        return codes.ToList();
    }

    // Every string property of every metadata section (StudyDesign, Publication, ..., Ontology),
    // e.g. d.InVivo.Species, d.Publication.PaperTitle. Built once.
    private static readonly (PropertyInfo Section, PropertyInfo Field)[] TextFields =
        typeof(Dataset).GetProperties()
            .Where(s => s.PropertyType.IsClass && s.PropertyType != typeof(string)
                        && s.PropertyType.Namespace?.StartsWith("Pidar.Models") == true)
            .SelectMany(s => s.PropertyType.GetProperties()
                .Where(f => f.PropertyType == typeof(string) && f.CanRead && f.GetIndexParameters().Length == 0)
                .Select(f => (s, f)))
            .ToArray();

    private static readonly MethodInfo RegexIsMatch =
        typeof(Regex).GetMethod(nameof(Regex.IsMatch), new[] { typeof(string), typeof(string), typeof(RegexOptions) })!;

    /// <summary>d => any text field of d matches the pattern (translated to SQL "field ~* pattern").</summary>
    public static Expression<Func<Dataset, bool>> AnyTextFieldMatches(string pattern) =>
        TextFieldsMatch(pattern, TextFields);

    /// <summary>d => any of the given fields matches the pattern.</summary>
    public static Expression<Func<Dataset, bool>> TextFieldsMatch(
        string pattern, IReadOnlyCollection<(PropertyInfo Section, PropertyInfo Field)> fields)
    {
        var d = Expression.Parameter(typeof(Dataset), "d");
        var patternExpr = Expression.Constant(pattern);
        var options = Expression.Constant(RegexOptions.IgnoreCase);

        var matches = fields
            .Select(x => (Expression)Expression.Call(RegexIsMatch,
                Expression.Property(Expression.Property(d, x.Section), x.Field), patternExpr, options))
            .ToList();

        return Expression.Lambda<Func<Dataset, bool>>(
            matches.Count == 0 ? Expression.Constant(false) : BalancedOr(matches, 0, matches.Count), d);
    }

    // Balanced OR tree (depth ~8 for 218 fields) instead of a 218-deep chain,
    // which keeps EF Core's expression visitors well away from recursion limits.
    private static Expression BalancedOr(List<Expression> items, int start, int count) =>
        count == 1
            ? items[start]
            : Expression.OrElse(BalancedOr(items, start, count / 2),
                                BalancedOr(items, start + count / 2, count - count / 2));

    // ------------------------------------------------------------------
    // SEARCH FIELDS (the dropdown next to the search bar)
    // ------------------------------------------------------------------

    /// <param name="Key">value sent as ?SearchField=</param>
    /// <param name="Text">text columns searched (null = all 218 fields; empty = none)</param>
    /// <param name="Categories">ontology categories (Ontology property names) whose codes count (null = all)</param>
    /// <param name="Values">which columns feed the suggestions (FieldRow accessors)</param>
    public sealed record SearchField(
        string Key, string Label, string Placeholder,
        (PropertyInfo Section, PropertyInfo Field)[]? Text,
        string[]? Categories,
        Func<FieldRow, string?>[] Values);

    /// <summary>The few columns used for suggestions, loaded in one small query.</summary>
    public sealed record FieldRow(int DatasetId, string? Species, string? DiseaseModel, string? OrganOrTissue,
                                  string? ImagingModality, string? ImagingSubModality);

    private static (PropertyInfo, PropertyInfo) Col(string section, string field)
    {
        var s = typeof(Dataset).GetProperty(section)!;
        return (s, s.PropertyType.GetProperty(field)!);
    }

    // Column accessors shared by the fields below (same instances, so a suggestion can find its field label)
    private static readonly Func<FieldRow, string?> GetSpecies = r => r.Species;
    private static readonly Func<FieldRow, string?> GetDisease = r => r.DiseaseModel;
    private static readonly Func<FieldRow, string?> GetOrgan = r => r.OrganOrTissue;
    private static readonly Func<FieldRow, string?> GetModality = r => r.ImagingModality;
    private static readonly Func<FieldRow, string?> GetSubModality = r => r.ImagingSubModality;

    public static readonly SearchField[] Fields =
    {
        new("all", "All fields", "Search all metadata, e.g. breast cancer mice", null, null,
            new[] { GetSpecies, GetDisease, GetOrgan, GetModality, GetSubModality }),
        new("synonyms", "Synonyms", "Ontology synonym, e.g. NP313 or isoflurane", Array.Empty<(PropertyInfo, PropertyInfo)>(), null,
            Array.Empty<Func<FieldRow, string?>>()),
        new("modality", "Imaging Modality", "e.g. PET, MRI, CT",
            new[] { Col("StudyComponent", "ImagingModality"), Col("StudyComponent", "ImagingSubModality") },
            new[] { "NcitImagingModality", "NcitImagingSubmodality" },
            new[] { GetModality, GetSubModality }),
        new("species", "Species", "e.g. mice, rats",
            new[] { Col("InVivo", "Species") }, new[] { "NcitSpecies" },
            new[] { GetSpecies }),
        new("disease", "Disease Model", "e.g. breast cancer",
            new[] { Col("InVivo", "DiseaseModel") }, new[] { "DoidDiseaseModel" },
            new[] { GetDisease }),
        new("organ", "Organ / Tissue", "e.g. mammary gland, brain",
            new[] { Col("InVivo", "OrganOrTissue") }, new[] { "UberonOrganOrTissue" },
            new[] { GetOrgan }),
    };

    public static SearchField GetField(string? key) =>
        Fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)) ?? Fields[0];

    /// <summary>
    /// Applies the search phrase to the query: every word must match.
    /// A word matches if it appears in the field's text columns OR one of its synonyms/typed codes
    /// is among the dataset's ontology codes (restricted to the field's ontology categories).
    /// </summary>
    public async Task<IQueryable<Dataset>> ApplySearchAsync(IQueryable<Dataset> query, string phrase, string? fieldKey = null)
    {
        var field = GetField(fieldKey);
        var textFields = field.Text ?? TextFields;
        var cats = field.Categories;

        foreach (var term in Tokenize(phrase))
        {
            var textMatch = TextFieldsMatch(WordPattern(term), textFields);
            var codes = await ResolveCodesForTermAsync(term);

            if (codes.Count == 0)
            {
                query = query.Where(textMatch);   // (for "synonyms" this is "false": no result)
                continue;
            }

            var d = textMatch.Parameters[0];
            Expression<Func<Dataset, bool>> ontoMatch = cats == null
                ? ds => _db.DatasetOntologyTerms.Any(t => t.DatasetId == ds.DatasetId && codes.Contains(t.Code.ToUpper()))
                : ds => _db.DatasetOntologyTerms.Any(t => t.DatasetId == ds.DatasetId && codes.Contains(t.Code.ToUpper())
                                                          && cats.Contains(t.Category));
            var ontoBody = new ReplaceParameter(ontoMatch.Parameters[0], d).Visit(ontoMatch.Body)!;

            var body = textFields.Length == 0 ? ontoBody : Expression.OrElse(textMatch.Body, ontoBody);
            query = query.Where(Expression.Lambda<Func<Dataset, bool>>(body, d));
        }

        return query;
    }

    // ------------------------------------------------------------------
    // SUGGESTIONS (autocomplete while typing)
    // ------------------------------------------------------------------

    public sealed record Suggestion(string Text, string Hint, int Datasets);

    /// <summary>
    /// Suggestions for the typed text in the chosen field: real values of that field (split on , and ;)
    /// and ontology synonyms, matched at the start of a word. Ranked: exact, starts-with, then by dataset count.
    /// </summary>
    public async Task<List<Suggestion>> SuggestAsync(string? fieldKey, string? q, int max = 10)
    {
        q = (q ?? "").Trim();
        if (q.Length == 0) return new();

        var field = GetField(fieldKey);
        var re = new Regex(@"(?<![\p{L}\p{N}])" + Regex.Escape(q), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var found = new Dictionary<string, (string Text, string Hint, HashSet<int> Ds, int Kind)>(StringComparer.OrdinalIgnoreCase);

        // 1) Values of the field's columns
        if (field.Values.Length > 0)
        {
            var rows = await _db.Datasets.AsNoTracking()
                .Select(d => new FieldRow(d.DatasetId,
                    d.InVivo != null ? d.InVivo.Species : null,
                    d.InVivo != null ? d.InVivo.DiseaseModel : null,
                    d.InVivo != null ? d.InVivo.OrganOrTissue : null,
                    d.StudyComponent != null ? d.StudyComponent.ImagingModality : null,
                    d.StudyComponent != null ? d.StudyComponent.ImagingSubModality : null))
                .ToListAsync();

            // accessor → field label ("Species", "Organ / Tissue", ...) for the hint
            var labelFor = Fields.Where(f => f.Key is not ("all" or "synonyms"))
                .SelectMany(f => f.Values.Select(v => (v, f.Label)))
                .ToDictionary(x => x.v, x => x.Label);

            foreach (var row in rows)
                foreach (var get in field.Values)
                {
                    var label = labelFor.TryGetValue(get, out var l) ? l : field.Label;
                    foreach (var part in Split(get(row)))
                    {
                        if (!re.IsMatch(part)) continue;
                        if (!found.TryGetValue(part, out var e))
                            found[part] = e = (part, label, new HashSet<int>(), 0);
                        e.Ds.Add(row.DatasetId);
                    }
                }
        }

        // 2) Ontology synonyms (codes restricted to the field's categories, except for "all"/"synonyms")
        var terms = await _db.DatasetOntologyTerms.AsNoTracking()
            .Select(t => new { t.DatasetId, t.Category, t.Code })
            .ToListAsync();
        var cats = field.Categories;
        var dsByCode = terms
            .Where(t => cats == null || cats.Contains(t.Category))
            .GroupBy(t => t.Code.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Select(x => x.DatasetId).ToHashSet());

        var synonyms = await _db.OntologySynonyms.AsNoTracking()
            .Select(s => new { s.Code, s.Synonym })
            .ToListAsync();

        foreach (var s in synonyms)
        {
            if (!re.IsMatch(s.Synonym)) continue;
            var code = s.Code.Trim().ToUpperInvariant();
            dsByCode.TryGetValue(code, out var ds);
            if ((ds == null || ds.Count == 0) && field.Key != "synonyms") continue;   // only useful synonyms outside the Synonyms field
            if (found.ContainsKey(s.Synonym)) continue;
            found[s.Synonym] = (s.Synonym, code, ds ?? new HashSet<int>(), 1);
        }

        return found.Values
            .OrderByDescending(x => x.Text.Equals(q, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Text.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Ds.Count)
            .ThenBy(x => x.Text.Length)
            .Take(max)
            .Select(x => new Suggestion(x.Text,
                $"{x.Hint} · {(x.Ds.Count == 0 ? "no datasets" : $"{x.Ds.Count} dataset{(x.Ds.Count == 1 ? "" : "s")}")}",
                x.Ds.Count))
            .ToList();
    }

    // "PET,CT,US" / "liver, kidney" → separate values
    private static IEnumerable<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Enumerable.Empty<string>()
            : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(p => Regex.Replace(p, @"\s+", " ").Trim().TrimEnd('.'))
                   .Where(p => p.Length > 0 && p.Length <= 80);

    private sealed class ReplaceParameter : ExpressionVisitor
    {
        private readonly ParameterExpression _from, _to;
        public ReplaceParameter(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
        protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : node;
    }
}
