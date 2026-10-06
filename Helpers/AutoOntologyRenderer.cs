using System.Net;
using Pidar.Helpers;

namespace Pidar.Helpers
{
    public static class AutoOntologyRenderer
    {
        public static string Render(string? codes)
        {
            if (string.IsNullOrWhiteSpace(codes)) return string.Empty;

            var html = "<div class='d-flex flex-wrap gap-2'>";
            foreach (var token in codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parsed = OntologyUrlHelper.Parse(token);
                if (parsed.Count == 1)
                {
                    var item = parsed[0];
                    html += $"<a href='{item.Url}' target='_blank' rel='noopener' " +
                            $"class='badge bg-primary text-light' " +
                            $"data-bs-toggle='tooltip' " +
                            $"title='Open {item.Code}'>{item.Code}</a>";
                }
                else
                {
                    // Not a recognisable PREFIX:ID code — show it as plain text instead of dropping it
                    html += $"<span>{WebUtility.HtmlEncode(token)}</span>";
                }
            }
            html += "</div>";

            return html;
        }
    }
}
