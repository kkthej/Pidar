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
    public static Expression<Func<Dataset, bool>> AnyTextFieldMatches(string pattern)
    {
        var d = Expression.Parameter(typeof(Dataset), "d");
        var patternExpr = Expression.Constant(pattern);
        var options = Expression.Constant(RegexOptions.IgnoreCase);

        var matches = TextFields
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

    /// <summary>Applies the search phrase to the query: every word must match text or ontology.</summary>
    public async Task<IQueryable<Dataset>> ApplySearchAsync(IQueryable<Dataset> query, string phrase)
    {
        foreach (var term in Tokenize(phrase))
        {
            var textMatch = AnyTextFieldMatches(WordPattern(term));
            var codes = await ResolveCodesForTermAsync(term);

            if (codes.Count == 0)
            {
                query = query.Where(textMatch);
                continue;
            }

            // text OR ontology, combined into one expression
            var d = textMatch.Parameters[0];
            Expression<Func<Dataset, bool>> ontoMatch = ds =>
                _db.DatasetOntologyTerms.Any(t => t.DatasetId == ds.DatasetId && codes.Contains(t.Code.ToUpper()));
            var ontoBody = new ReplaceParameter(ontoMatch.Parameters[0], d).Visit(ontoMatch.Body)!;

            query = query.Where(Expression.Lambda<Func<Dataset, bool>>(
                Expression.OrElse(textMatch.Body, ontoBody), d));
        }

        return query;
    }

    private sealed class ReplaceParameter : ExpressionVisitor
    {
        private readonly ParameterExpression _from, _to;
        public ReplaceParameter(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
        protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : node;
    }
}
