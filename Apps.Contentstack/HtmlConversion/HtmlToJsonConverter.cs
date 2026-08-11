using System.Text;
using System.Web;
using Apps.Contentstack.HtmlConversion.Constants;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class HtmlToJsonConverter
{
    private const string ContentTypeMetaTag = "blackbird-content-type-id";
    private const string EntryMetaTag = "blackbird-entry-id";

    public static List<string> UpdateEntryFromHtml(Stream file, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);

        try
        {
            return ApplyHtmlToEntry(doc, entry, logger);
        }
        catch(Exception ex)
        {
            logger?.LogError.Invoke($"Conversion to Contentstack JSON failed. Entry json: {entry}; HTML: {doc.DocumentNode.OuterHtml}; Exception: {ex}", null);
            throw new PluginMisconfigurationException("The HTML file structure should match the source article");
        }
    }

    public static List<(string ContentTypeId, string EntryId)> ExtractReferencedEntryIds(Stream file)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);
        file.Position = 0;

        return doc.DocumentNode
            .SelectNodes($"//article[@{ConversionConstants.RefContentTypeAttr}]")
            ?.Select(node => (
                node.GetAttributeValue(ConversionConstants.RefContentTypeAttr, string.Empty),
                node.GetAttributeValue(ConversionConstants.RefEntryIdAttr, string.Empty)
            ))
            .Where(x => !string.IsNullOrEmpty(x.Item1) && !string.IsNullOrEmpty(x.Item2))
            .Distinct()
            .ToList()
            ?? new List<(string, string)>();
    }

    public static List<string> UpdateReferencedEntryFromHtml(Stream file, string contentTypeId, string entryId, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);
        file.Position = 0;

        var articleNode = doc.DocumentNode.SelectSingleNode(
            $"//article[@{ConversionConstants.RefContentTypeAttr}='{contentTypeId}' and @{ConversionConstants.RefEntryIdAttr}='{entryId}']");

        if (articleNode is null)
            return [];

        var tempDoc = new HtmlDocument();
        tempDoc.LoadHtml($"<body>{articleNode.InnerHtml}</body>");

        try
        {
            return ApplyHtmlToEntry(tempDoc, entry, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError.Invoke($"Failed to update referenced entry {entryId}: {ex}", null);
            throw;
        }
    }

    private static List<string> ApplyHtmlToEntry(HtmlDocument doc, JObject entry, Logger? logger)
    {
        var errors = new List<string>();

        var entryNodes = doc.DocumentNode.Descendants()
            .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null &&
                        !x.Ancestors().Any(a => a.Name == "article"))
            .ToList();

        var jsonRichTextNodes = entryNodes
            .Where(x => x.Attributes[ConversionConstants.BlackbirdJsonValue] is not null)
            .ToList();

        if (jsonRichTextNodes.Count > 0)
        {
            var claimed = jsonRichTextNodes
                .SelectMany(x => x.DescendantsAndSelf())
                .ToHashSet();

            entryNodes = entryNodes.Where(x => !claimed.Contains(x)).ToList();

            foreach (var node in jsonRichTextNodes)
                ApplyJsonRichText(node, entry, logger, errors);
        }

        var repeatableNodes = entryNodes
            .Where(x => x.SelectNodes($"./div[@class='{ConversionConstants.MultipleItemClass}']") is { Count: > 0 })
            .ToList();

        foreach (var node in repeatableNodes)
        {
            var path = node.Attributes[ConversionConstants.PathAttr].Value!;
            var arrayToken = entry.SelectToken(path) as JArray;

            if (arrayToken == null)
            {
                logger?.LogWarning.Invoke($"Path {path} not found or is not an array in the entry", null);
                continue;
            }

            var multipleItems = node.SelectNodes($"./div[@class='{ConversionConstants.MultipleItemClass}']").ToList();

            if (multipleItems.Count != arrayToken.Count)
            {
                logger?.LogWarning.Invoke($"Mismatch in array lengths for path {path}. HTML has {multipleItems.Count} items, JSON has {arrayToken.Count} items", null);
            }

            for (int i = 0; i < Math.Min(multipleItems.Count, arrayToken.Count); i++)
            {
                var itemValue = ExtractValue(multipleItems[i]);
                if (arrayToken[i] is JValue jValue)
                    jValue.Value = itemValue;
                else
                    arrayToken[i] = itemValue;
            }
        }

        entryNodes.ForEach(x =>
        {
            var path = x.Attributes[ConversionConstants.PathAttr].Value!;

            if (x.Attributes[ConversionConstants.BlackbirdFieldType]?.Value == ConversionConstants.FileFieldType)
            {
                var uid = x.Attributes[ConversionConstants.BlackbirdFileUid]?.Value;
                if (!string.IsNullOrEmpty(uid))
                    SetFileUidAtPath(entry, path, uid);
                return;
            }

            var propertyValue = entry.SelectToken(path);
            if (propertyValue == null)
                return;

            if (propertyValue is JValue jValue)
                jValue.Value = ExtractValue(x);
        });

        return errors;
    }

    private static void ApplyJsonRichText(HtmlNode container, JObject entry, Logger? logger,
        ICollection<string> errors)
    {
        var fieldPath = container.Attributes[ConversionConstants.PathAttr].Value!;
        var encoded = container.Attributes[ConversionConstants.BlackbirdJsonValue].Value;

        JToken source;
        try
        {
            source = JToken.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
        }
        catch (Exception ex)
        {
            Report(errors, logger,
                $"Rich text field '{fieldPath}' was left unchanged: the source value embedded in the file could not be read ({ex.Message}).");
            return;
        }

        foreach (var node in container.Descendants()
                     .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null))
        {
            var path = node.Attributes[ConversionConstants.PathAttr].Value!;
            var relativePath = ToRelativePath(fieldPath, path);

            if (relativePath is null || source.SelectToken(relativePath) is not JValue target)
            {
                Report(errors, logger,
                    $"Rich text field '{fieldPath}': the file carries text for '{path}', which does not exist in the exported source value. That text was not imported.");
                continue;
            }

            var value = ExtractValue(node);

            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(target.Value<string>()))
            {
                Report(errors, logger,
                    $"Rich text field '{fieldPath}': the file carries an empty value for '{path}', so the source text was kept instead of blanking it.");
                continue;
            }

            target.Value = value;
        }

        SetTokenAtPath(entry, fieldPath, source);
    }

    private static string? ToRelativePath(string fieldPath, string path)
    {
        if (path == fieldPath)
            return string.Empty;

        if (!path.StartsWith(fieldPath, StringComparison.Ordinal))
            return null;

        var remainder = path[fieldPath.Length..];
        return remainder.StartsWith('.') ? remainder[1..] : null;
    }

    private static void Report(ICollection<string> errors, Logger? logger, string message)
    {
        errors.Add(message);
        logger?.LogWarning.Invoke(message, null);
    }

    private static string ExtractValue(HtmlNode node)
    {
        switch (node.Attributes[ConversionConstants.BlackbirdFieldType]?.Value)
        {
            case ConversionConstants.RichTextNodeFieldType:
                return HttpUtility.HtmlDecode(node.InnerText);
            
            case ConversionConstants.HtmlFieldType:
                return node.InnerHtml.Trim();
        }

        var innerHtml = node.Name == HtmlConstants.Span ? node.InnerHtml : node.InnerHtml.Trim();
        return HttpUtility.HtmlDecode(innerHtml);
    }

    private static void SetFileUidAtPath(JObject entry, string path, string uid)
        => SetTokenAtPath(entry, path, new JValue(uid));

    private static void SetTokenAtPath(JObject entry, string path, JToken newValue)
    {
        var existing = entry.SelectToken(path);
        if (existing != null)
        {
            existing.Replace(newValue);
            return;
        }

        var segments = ParseJPathSegments(path);
        JToken current = entry;
        for (int i = 0; i < segments.Count - 1; i++)
        {
            var (name, index) = segments[i];
            if (index.HasValue)
            {
                var parent = (JObject)current;
                if (parent[name] is not JArray arr)
                {
                    arr = new JArray();
                    parent[name] = arr;
                }
                while (arr.Count <= index.Value)
                    arr.Add(new JObject());
                current = arr[index.Value]!;
            }
            else
            {
                var parent = (JObject)current;
                if (parent[name] is JObject nested)
                    current = nested;
                else
                {
                    var created = new JObject();
                    parent[name] = created;
                    current = created;
                }
            }
        }

        var (lastName, lastIndex) = segments[^1];

        if (!lastIndex.HasValue)
        {
            ((JObject)current)[lastName] = newValue;
            return;
        }

        var container = (JObject)current;
        if (container[lastName] is not JArray lastArray)
        {
            lastArray = new JArray();
            container[lastName] = lastArray;
        }
        while (lastArray.Count <= lastIndex.Value)
            lastArray.Add(JValue.CreateNull());
        lastArray[lastIndex.Value] = newValue;
    }

    private static List<(string Name, int? Index)> ParseJPathSegments(string path)
    {
        var segments = new List<(string, int?)>();
        foreach (var segment in path.Split('.'))
        {
            var bracket = segment.IndexOf('[');
            if (bracket >= 0 && segment.EndsWith(']'))
            {
                var name = segment[..bracket];
                var index = int.Parse(segment[(bracket + 1)..^1]);
                segments.Add((name, index));
            }
            else
            {
                segments.Add((segment, null));
            }
        }
        return segments;
    }

    public static (string? ContentTypeId, string? EntryId) ExtractContentTypeAndEntryId(Stream file)
    {
        var doc = new HtmlDocument();
        doc.Load(file);

        var contentTypeId = doc.DocumentNode.SelectSingleNode($"//meta[@name='{ContentTypeMetaTag}']")?.GetAttributeValue("content", null);
        var entryId = doc.DocumentNode.SelectSingleNode($"//meta[@name='{EntryMetaTag}']")?.GetAttributeValue("content", null);
        
        file.Position = 0;
        return (contentTypeId, entryId);
    }
}
