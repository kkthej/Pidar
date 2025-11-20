using Microsoft.AspNetCore.Html;

namespace Pidar.Helpers
{
    public static class RazorFieldHelper
    {
        public static object? GetNested(object? root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
                return null;

            var parts = path.Split(".");
            object? current = root;

            foreach (var part in parts)
            {
                if (current == null) return null;

                var prop = current.GetType().GetProperty(part);
                if (prop == null) return null;

                current = prop.GetValue(current);
            }

            return current;
        }

        public static IHtmlContent FormatLabel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return HtmlString.Empty;

            name = name.Replace("/", " ");

            var formatted = System.Text.RegularExpressions.Regex
                .Replace(name, @"(?<=[a-z0-9])(?=[A-Z][a-z])", " ")
                .Trim();

            if (formatted.Length > 0)
                formatted = char.ToUpper(formatted[0]) + formatted.Substring(1);

            return new HtmlString(formatted);
        }

        public static bool IsLong(string field)
        {
            return field.Contains("Background")
                || field.Contains("Description")
                || field.Contains("Procedure")
                || field.Contains("Details")
                || field.Contains("Protocol")
                || field.Contains("Notes")
                || field.Contains("Justification")
                || field.Contains("Methods");
        }
    }
}
