namespace Apps.Contentstack.Models.Entities;

public class AssetEntity
{
    public string Uid { get; set; } = string.Empty;

    public string Filename { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }

    public string Url { get; set; } = string.Empty;

    public IEnumerable<string> GetNames()
    {
        return new[] { Filename, Title }.Where(x => !string.IsNullOrWhiteSpace(x));
    }
    
    public string? BuildTargetName(string search, string replacement)
    {
        return GetNames()
            .FirstOrDefault(x => x.Contains(search, StringComparison.OrdinalIgnoreCase))?
            .Replace(search, replacement, StringComparison.OrdinalIgnoreCase);
    }
}