using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Helper;

public static class EntryReferenceExtractor
{
    public static List<string> ExtractReferencedEntryUids(JObject entry, ContentTypeBlockEntity contentType)
    {
        var referencedEntryUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ExtractReferencedEntryUidsFromSchema(entry, contentType.Schema, referencedEntryUids);
        return referencedEntryUids.ToList();
    }
    
    public static HashSet<(string EntryUid, string ContentTypeUid)> ExtractReferencedEntriesWithContentTypes(
        JObject entry,
        ContentTypeBlockEntity contentType,
        IEnumerable<string>? excludedContentTypeIds = null)
    {
        var results = new HashSet<(string, string)>();
        ExtractReferencedEntriesFromSchema(entry, contentType.Schema, results);
        return ContentTypeExclusionFilter.FilterReferencedEntries(results, excludedContentTypeIds);
    }

    private static void ExtractReferencedEntryUidsFromSchema(JObject entry, JArray schema, ISet<string> referencedEntryUids)
    {
        foreach (var schemaToken in schema.OfType<JObject>())
        {
            var field = schemaToken.ToObject<EntryProperty>();
            var fieldUid = schemaToken["uid"]?.ToString();

            if (field is null || string.IsNullOrWhiteSpace(fieldUid))
                continue;

            field.Uid = fieldUid;
            var property = entry[fieldUid];
            if (property is null)
                continue;

            ExtractReferencedEntryUidsFromProperty(property, field, referencedEntryUids);
        }
    }

    private static void ExtractReferencedEntryUidsFromProperty(JToken property, EntryProperty field, ISet<string> referencedEntryUids)
    {
        switch (field.DataType)
        {
            case "reference":
                AddReferenceUids(property, referencedEntryUids);
                break;
            case "group":
            case "global_field":
                if (field.Schema is null)
                    break;

                if (property is JObject propertyObject)
                {
                    ExtractReferencedEntryUidsFromSchema(propertyObject, field.Schema, referencedEntryUids);
                }
                else if (property is JArray propertyArray)
                {
                    foreach (var item in propertyArray.OfType<JObject>())
                    {
                        ExtractReferencedEntryUidsFromSchema(item, field.Schema, referencedEntryUids);
                    }
                }
                break;
            case "blocks":
                if (field.Blocks is null || property is not JArray blocksArray)
                    break;

                foreach (var blockItem in blocksArray.OfType<JObject>())
                {
                    var blockProperty = blockItem.Properties().FirstOrDefault();
                    if (blockProperty?.Value is not JObject blockValue)
                        continue;

                    var blockSchema = field.Blocks.FirstOrDefault(x => x.Uid == blockProperty.Name)?.Schema;
                    if (blockSchema is not null)
                    {
                        ExtractReferencedEntryUidsFromSchema(blockValue, blockSchema, referencedEntryUids);
                    }
                }
                break;
        }
    }

    private static void AddReferenceUids(JToken property, ISet<string> referencedEntryUids)
    {
        if (property is JArray propertyArray)
        {
            foreach (var item in propertyArray)
            {
                AddReferenceUid(item, referencedEntryUids);
            }

            return;
        }

        AddReferenceUid(property, referencedEntryUids);
    }

    private static void AddReferenceUid(JToken referenceToken, ISet<string> referencedEntryUids)
    {
        var uid = referenceToken.Type switch
        {
            JTokenType.String => referenceToken.ToString(),
            JTokenType.Object => referenceToken["uid"]?.ToString() ?? referenceToken["entry_uid"]?.ToString(),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(uid))
        {
            referencedEntryUids.Add(uid);
        }
    }

    private static void ExtractReferencedEntriesFromSchema(JObject entry, JArray schema, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        foreach (var schemaToken in schema.OfType<JObject>())
        {
            var field = schemaToken.ToObject<EntryProperty>();
            var fieldUid = schemaToken["uid"]?.ToString();

            if (field is null || string.IsNullOrWhiteSpace(fieldUid))
                continue;

            field.Uid = fieldUid;
            var property = entry[fieldUid];
            if (property is null)
                continue;

            ExtractReferencedEntriesFromProperty(property, field, results);
        }
    }

    private static void ExtractReferencedEntriesFromProperty(JToken property, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        switch (field.DataType)
        {
            case "reference":
                AddReferencesWithContentTypes(property, field, results);
                break;
            case "group":
            case "global_field":
                if (field.Schema is null)
                    break;

                if (property is JObject propertyObject)
                    ExtractReferencedEntriesFromSchema(propertyObject, field.Schema, results);
                else if (property is JArray propertyArray)
                    foreach (var item in propertyArray.OfType<JObject>())
                        ExtractReferencedEntriesFromSchema(item, field.Schema, results);
                break;
            case "blocks":
                if (field.Blocks is null || property is not JArray blocksArray)
                    break;

                foreach (var blockItem in blocksArray.OfType<JObject>())
                {
                    var blockProperty = blockItem.Properties().FirstOrDefault();
                    if (blockProperty?.Value is not JObject blockValue)
                        continue;

                    var blockSchema = field.Blocks.FirstOrDefault(x => x.Uid == blockProperty.Name)?.Schema;
                    if (blockSchema is not null)
                        ExtractReferencedEntriesFromSchema(blockValue, blockSchema, results);
                }
                break;
        }
    }

    private static void AddReferencesWithContentTypes(JToken property, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        if (property is JArray propertyArray)
        {
            foreach (var item in propertyArray)
                AddReferenceWithContentType(item, field, results);
            return;
        }

        AddReferenceWithContentType(property, field, results);
    }

    private static void AddReferenceWithContentType(JToken referenceToken, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        var uid = referenceToken.Type switch
        {
            JTokenType.String => referenceToken.ToString(),
            JTokenType.Object => referenceToken["uid"]?.ToString() ?? referenceToken["entry_uid"]?.ToString(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(uid))
            return;

        var contentTypeUid = referenceToken.Type == JTokenType.Object
            ? referenceToken["_content_type_uid"]?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(contentTypeUid))
        {
            var fallbackContentTypeIds = GetReferenceContentTypeIds(field).ToList();
            if (fallbackContentTypeIds.Count == 1)
                contentTypeUid = fallbackContentTypeIds[0];
        }

        if (!string.IsNullOrWhiteSpace(uid) && !string.IsNullOrWhiteSpace(contentTypeUid))
            results.Add((uid, contentTypeUid));
    }

    private static IEnumerable<string?> GetReferenceContentTypeIds(EntryProperty field)
    {
        if (field.ReferenceTo is null)
            return [];

        return field.ReferenceTo.Type switch
        {
            JTokenType.String => [field.ReferenceTo.ToString()],
            JTokenType.Array => field.ReferenceTo.Values<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase),
            _ => []
        };
    }
}
