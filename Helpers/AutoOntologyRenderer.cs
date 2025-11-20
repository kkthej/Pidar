using Microsoft.AspNetCore.Html;
using Pidar.Helpers;

namespace Pidar.Helpers
{
    public static class AutoOntologyRenderer
    {
        public static string Render(string? codes)
        {
            var items = OntologyUrlHelper.Parse(codes);

            var html = "<div class='d-flex flex-wrap gap-2'>";
            foreach (var item in items)
            {
                html += $"<a href='{item.Url}' target='_blank' " +
                        $"class='badge bg-primary text-light' " +
                        $"data-bs-toggle='tooltip' " +
                        $"title='Open {item.Code}'>{item.Code}</a>";
            }
            html += "</div>";

            return html;
        }
    }
}
