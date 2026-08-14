using System.Text;

namespace Apps.Contentstack.Helper;

public static class FileNameHelper
{
    private const int MaxBaseNameLength = 100;

    private static readonly char[] InvalidCharacters = Path.GetInvalidFileNameChars()
        .Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '#', '%', '&', '{', '}', '$', '\'', '`' })
        .Distinct()
        .ToArray();
    
    public static string SanitizeBaseName(string? baseName, string? fallbackBaseName = null)
    {
        var sanitized = Clean(baseName);

        if (string.IsNullOrEmpty(sanitized))
            sanitized = Clean(fallbackBaseName);

        return string.IsNullOrEmpty(sanitized) ? "entry" : sanitized;
    }

    private static string Clean(string? value)
    {
        var builder = new StringBuilder();

        foreach (var character in value ?? string.Empty)
            builder.Append(char.IsControl(character) || InvalidCharacters.Contains(character) ? ' ' : character);

        var cleaned = string.Join(" ", builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Trim('.', ' ');

        if (cleaned.Length > MaxBaseNameLength)
            cleaned = cleaned[..MaxBaseNameLength].Trim('.', ' ');

        return cleaned;
    }
}
