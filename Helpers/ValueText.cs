namespace Pidar.Helpers
{
    /// <summary>
    /// Display formatting for metadata VALUES (not field labels).
    /// Values are shown as entered: no '/' removal and no camelCase splitting,
    /// so "Balb/c", "mg/kg", "PET/CT", "MicroCT" stay intact.
    /// </summary>
    public static class ValueText
    {
        public static string Display(string? value)
        {
            var s = value?.Trim();
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // Capitalise only a plain lower-case word at the start ("yes" -> "Yes",
            // "breast cancer" -> "Breast cancer"). Leave symbols and mixed-case terms
            // alone: "n/a", "pH 7.4", "mRNA", "iPSC", "e.g." are kept as written.
            if (s.Length > 1 && char.IsLower(s[0]) && char.IsLower(s[1]))
            {
                int end = 0;
                while (end < s.Length && char.IsLetter(s[end])) end++;
                var firstWord = s[..end];
                bool plainWord = firstWord.All(char.IsLower) &&
                                 (end == s.Length || s[end] == ' ' || s[end] == ',' || s[end] == ';');
                if (plainWord)
                    s = char.ToUpper(s[0]) + s[1..];
            }

            return s;
        }
    }
}
