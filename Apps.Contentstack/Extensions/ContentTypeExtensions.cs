using Apps.Contentstack.Models;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Extensions;

public static class ContentTypeExtensions
{
    public static List<EntryProperty> GetLocalizableFields(this JArray schema)
    {
        return schema
            .Select(x =>
            {
                var field = x.ToObject<EntryProperty>()!;
                field.Uid = x["uid"]!.Value<string>();
                field.ContentTypeSchema = schema.FirstOrDefault(y => y["uid"]!.Value<string>() == field.Uid) as JObject;
                return field;
            })
            .Where(x => !x.NonLocalizable)
            .ToList();
    }
}