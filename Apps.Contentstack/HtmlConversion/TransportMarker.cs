using System.Text.RegularExpressions;

namespace Apps.Contentstack.HtmlConversion;

public static partial class TransportMarker
{
    [GeneratedRegex(
        """(?:<|&lt;|&#60;|%3C)\s*/?\s*(?:span|div|p|a)\b[^>]*?(?:path|data-blackbird-key|data-blackbird-field-type|data-blackbird-json-value)\s*=""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MarkupPattern();

    public static bool IsPresentIn(string? value)
        => !string.IsNullOrEmpty(value) && MarkupPattern().IsMatch(value);

    public static string Describe(string value)
    {
        var match = MarkupPattern().Match(value);
        var start = Math.Max(0, match.Index - 20);
        var excerpt = value[start..Math.Min(value.Length, start + 120)];

        return start > 0 ? "…" + excerpt : excerpt;
    }
}
