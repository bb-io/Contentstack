using Apps.Contentstack.Models;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Extensions;

public static class ContentTypeExtensions
{
    public static List<EntryProperty> GetLocalizableFields(this JArray schema)
    {
        return schema
            .Select(x => x.ToObject<EntryProperty>()!)
            .Where(x => !x.NonLocalizable)
            .ToList();
    }
}