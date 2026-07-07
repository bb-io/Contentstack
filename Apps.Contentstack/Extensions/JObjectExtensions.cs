using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Extensions;

public static class JObjectExtensions
{
    public static bool IsAssetObject(this JObject obj)
    {
        var uid = obj["uid"]?.ToString();
        var filename = obj["filename"]?.ToString();
        return !string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(filename);
    }
    
    public static List<string> ExtractAssetIds(this JObject? entryJObject, string? fileExtension)
    {
        if (entryJObject is null)
            return [];
        
        return entryJObject
            .Descendants()
            .OfType<JObject>()
            .Where(x => x.IsAssetObject())
            .Where(x => 
                string.IsNullOrEmpty(fileExtension) || 
                (x["filename"]?.ToString().EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(x => x["uid"]!.ToString())
            .Distinct()
            .ToList();
    }
}