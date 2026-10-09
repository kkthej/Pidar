namespace Pidar.Helpers
{
    /// <summary>
    /// Statistics page, "Country of imaging facility": each country's bar is drawn in its flag colours.
    /// Returns the CSS class (wwwroot/css/site.css, "COUNTRY FLAG BARS"), e.g. "Italy" → "flag-it".
    /// Countries not listed here keep the default blue bar; to add one, add a line here and a class in site.css.
    /// </summary>
    public static class CountryFlags
    {
        private static readonly Dictionary<string, string> Codes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Italy"] = "it", ["Italia"] = "it",
            ["Hungary"] = "hu",
            ["Belgium"] = "be",
            ["Czech Republic"] = "cz", ["Czechia"] = "cz",
            ["Norway"] = "no",
            ["Portugal"] = "pt",
            ["Germany"] = "de",
            ["France"] = "fr",
            ["Spain"] = "es",
            ["Netherlands"] = "nl", ["The Netherlands"] = "nl",
            ["Austria"] = "at",
            ["Poland"] = "pl",
            ["Ireland"] = "ie",
            ["Sweden"] = "se",
            ["Finland"] = "fi",
            ["Denmark"] = "dk",
        };

        public static string? CssClass(string? country) =>
            country != null && Codes.TryGetValue(country.Trim(), out var code) ? "flag-" + code : null;
    }
}
