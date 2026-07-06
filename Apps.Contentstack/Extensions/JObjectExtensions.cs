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
}