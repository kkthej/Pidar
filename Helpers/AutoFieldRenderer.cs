using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pidar.Helpers;

namespace Pidar.Helpers
{
    public static class AutoFieldRenderer
    {
        public static IHtmlContent RenderAutoField(this IHtmlHelper html, string fieldName, object? fieldValue)
        {
            // Detect Ontology fields (Ontology.*)
            if (fieldName.StartsWith("Ontology.", StringComparison.OrdinalIgnoreCase))
            {
                return RenderOntologyField(fieldValue?.ToString());
            }

            // Normal text fallback
            return new HtmlString(fieldValue?.ToString() ?? "<span class='text-muted'>—</span>");
        }

        private static IHtmlContent RenderOntologyField(string? codes)
        {
            var items = OntologyUrlHelper.Parse(codes);

            var div = new TagBuilder("div");
            div.AddCssClass("d-flex flex-wrap gap-2");

            foreach (var (Code, Url) in items)
            {
                var a = new TagBuilder("a");
                a.AddCssClass("badge bg-primary text-light");
                a.Attributes["href"] = Url;
                a.Attributes["target"] = "_blank";
                a.Attributes["data-bs-toggle"] = "tooltip";
                a.Attributes["title"] = $"Open {Code}";
                a.InnerHtml.Append(Code);
                div.InnerHtml.AppendHtml(a);
            }

            return div;
        }
    }
}
