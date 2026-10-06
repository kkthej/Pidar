using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pidar.Helpers
{
    public static class OntologyUrlHelper
    {
        // PREFIX + separator + local id, e.g. NCIT:C14238, DUO:0000042, CLO_0001794, DUO-0000042.
        // Accepting '_' and '-' keeps older/hand-entered values clickable; the code is
        // always displayed and linked in the canonical "PREFIX:ID" form.
        private static readonly Regex CodePattern =
            new(@"^(?<prefix>[A-Za-z][A-Za-z0-9]*)[:_\-](?<id>[A-Za-z]*\d[A-Za-z0-9]*)$", RegexOptions.Compiled);

        public static List<(string Code, string Url)> Parse(string? codes)
        {
            var list = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(codes)) return list;

            foreach (var c in codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var m = CodePattern.Match(c.Trim(' ', ' '));
                if (!m.Success) continue;

                var prefix = m.Groups["prefix"].Value.ToUpperInvariant();
                var id = m.Groups["id"].Value;

                list.Add(($"{prefix}:{id}", $"https://purl.obolibrary.org/obo/{prefix}_{id}"));
            }

            return list;
        }
    }
}
