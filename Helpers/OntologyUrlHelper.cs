using System.Collections.Generic;

namespace Pidar.Helpers
{
    public static class OntologyUrlHelper
    {
        public static List<(string Code, string Url)> Parse(string? codes)
        {
            var list = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(codes)) return list;

            foreach (var c in codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = c.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;

                var prefix = parts[0].ToUpper();
                var id = parts[1];

                list.Add((c, $"http://purl.obolibrary.org/obo/{prefix}_{id}"));
            }

            return list;
        }
    }
}
