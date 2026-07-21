using System.Text.RegularExpressions;

namespace MLib.Texturing.Hotspotting
{
    public static class HotspotNameMatching
    {
        /// <summary>
        /// Checks whether the given name pattern contains unescaped wildcards (*, not \*).
        /// </summary>
        public static bool HasWildcards(string namePattern)
            => Regex.IsMatch(namePattern, @"(?<!\\)\*");    // Matches * but not \*

        /// <summary>
        /// Creates a regular expression for the given name pattern.
        /// </summary>
        public static Regex MakeNamePatternRegex(string namePattern)
        {
            var regex = Regex.Replace(namePattern, @"\\\*|\*|\\|[^\*\\]*", match =>
            {
                switch (match.Value)
                {
                    case @"*": return "(.*?)";                  // A wildcard can be anything (including empty)
                    case @"\*": return Regex.Escape("*");       // A literal * must be escaped (\*)
                    default: return Regex.Escape(match.Value);  // There are no other special characters
                }
            });
            return new Regex("^" + regex + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Checks whether the given fallback texture name contains placeholders ({0}, {1}, etc.).
        /// </summary>
        public static bool ContainsPlaceholders(string fallbackTextureName)
            => Regex.IsMatch(fallbackTextureName, @"\{\d+\}");

        /// <summary>
        /// Replaces placeholders in the given string with replacement values.
        /// For example, "wall_{0}_{1}" and ["large", "red"] becomes "wall_large_red".
        /// </summary>
        public static string ReplacePlaceholders(string value, string[] replacementValues)
        {
            return Regex.Replace(value, @"\{(\d+)\}", match =>
            {
                var index = int.Parse(match.Groups[1].Value);
                if (index < 0 || index >= replacementValues.Length)
                    return "";
                else
                    return replacementValues[index];
            });
        }
    }
}
