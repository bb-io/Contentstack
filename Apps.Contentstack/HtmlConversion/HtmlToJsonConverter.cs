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

    public static EntryImportReport UpdateEntryFromHtml(Stream file, JObject entry, Logger? logger)
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

    public static EntryImportReport UpdateReferencedEntryFromHtml(Stream file, string contentTypeId, string entryId, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);
        file.Position = 0;

        var articleNode = doc.DocumentNode.SelectSingleNode(
            $"//article[@{ConversionConstants.RefContentTypeAttr}='{contentTypeId}' and @{ConversionConstants.RefEntryIdAttr}='{entryId}']");

        if (articleNode is null)
            return new EntryImportReport();

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

    private static EntryImportReport ApplyHtmlToEntry(HtmlDocument doc, JObject entry, Logger? logger)
    {
        var report = new EntryImportReport();
        var errors = report.Errors;

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
                Report(errors, logger,
                    $"Field '{path}' was not imported: it does not exist as a list in the entry being updated.");
                continue;
            }

            var multipleItems = node.SelectNodes($"./div[@class='{ConversionConstants.MultipleItemClass}']").ToList();

            if (multipleItems.Count != arrayToken.Count)
            {
                Report(errors, logger,
                    $"Field '{path}': the file carries {multipleItems.Count} items but the entry has {arrayToken.Count}. Only the first {Math.Min(multipleItems.Count, arrayToken.Count)} were imported.");
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

        foreach (var node in entryNodes)
        {
            var path = node.Attributes[ConversionConstants.PathAttr].Value!;

            if (node.Attributes[ConversionConstants.BlackbirdFieldType]?.Value == ConversionConstants.FileFieldType)
            {
                var uid = node.Attributes[ConversionConstants.BlackbirdFileUid]?.Value;
                if (!string.IsNullOrEmpty(uid))
                    SetFileUidAtPath(entry, path, uid);
                continue;
            }

            var propertyValue = entry.SelectToken(path);

            if (propertyValue is JValue jValue)
            {
                jValue.Value = ExtractValue(node);
                continue;
            }

            if (propertyValue != null || IsContainer(node))
                continue;

            var value = ExtractValue(node);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (TrySetTokenAtPath(entry, path, new JValue(value), out var failure))
                report.CreatedFields.Add(path);
            else
                Report(errors, logger, failure!);
        }

        return report;
    }

    private static bool IsContainer(HtmlNode node)
        => node.Descendants().Any(x => x.Attributes[ConversionConstants.PathAttr] is not null)
           || node.ChildNodes.Any(x => x.GetAttributeValue("class", string.Empty) is
               ConversionConstants.MultipleItemClass or ConversionConstants.MultipleComplexItemClass);

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
        => TrySetTokenAtPath(entry, path, new JValue(uid), out _);

    private static void SetTokenAtPath(JObject entry, string path, JToken newValue)
        => TrySetTokenAtPath(entry, path, newValue, out _);

    private static bool TrySetTokenAtPath(JObject entry, string path, JToken newValue, out string? failure)
    {
        failure = null;

        var existing = entry.SelectToken(path);
        if (existing != null)
        {
            existing.Replace(newValue);
            return true;
        }

        if (!TryParseJPathSegments(path, out var segments))
        {
            failure = $"Field '{path}' was not imported: the file references a field path this app cannot interpret.";
            return false;
        }

        JToken current = entry;
        for (int i = 0; i < segments.Count - 1; i++)
        {
            var (name, index) = segments[i];

            if (current is not JObject parent)
            {
                failure = $"Field '{path}' was not imported: '{name}' cannot be created because the entry holds a value where a group was expected.";
                return false;
            }

            if (!index.HasValue)
            {
                if (parent[name] is JObject nested)
                {
                    current = nested;
                    continue;
                }

                if (parent[name] is not null and not JObject)
                {
                    failure = $"Field '{path}' was not imported: '{name}' already holds a value that is not a group.";
                    return false;
                }

                var created = new JObject();
                parent[name] = created;
                current = created;
                continue;
            }

            if (parent[name] is not JArray arr || index.Value >= arr.Count)
            {
                failure = $"Field '{path}' was not imported: the entry has no item {index.Value} in '{name}'.";
                return false;
            }

            if (arr[index.Value] is not JObject item)
            {
                failure = $"Field '{path}' was not imported: item {index.Value} of '{name}' is not a group.";
                return false;
            }

            current = item;
        }

        if (current is not JObject container)
        {
            failure = $"Field '{path}' was not imported: the entry holds a value where a group was expected.";
            return false;
        }

        var (lastName, lastIndex) = segments[^1];

        if (!lastIndex.HasValue)
        {
            container[lastName] = newValue;
            return true;
        }

        if (container[lastName] is not JArray lastArray)
        {
            if (container[lastName] is not null)
            {
                failure = $"Field '{path}' was not imported: '{lastName}' already holds a value that is not a list.";
                return false;
            }

            lastArray = new JArray();
            container[lastName] = lastArray;
        }

        if (lastIndex.Value > lastArray.Count)
        {
            failure = $"Field '{path}' was not imported: the entry's '{lastName}' list has {lastArray.Count} items, so item {lastIndex.Value} cannot be filled without leaving gaps.";
            return false;
        }

        if (lastIndex.Value == lastArray.Count)
            lastArray.Add(newValue);
        else
            lastArray[lastIndex.Value] = newValue;

        return true;
    }

    private static bool TryParseJPathSegments(string path, out List<(string Name, int? Index)> segments)
    {
        segments = [];

        if (string.IsNullOrWhiteSpace(path))
            return false;

        foreach (var segment in path.Split('.'))
        {
            var bracket = segment.IndexOf('[');
            if (bracket < 0)
            {
                if (segment.Length == 0)
                    return false;

                segments.Add((segment, null));
                continue;
            }

            if (!segment.EndsWith(']') || bracket == 0 ||
                !int.TryParse(segment[(bracket + 1)..^1], out var index) || index < 0)
                return false;

            segments.Add((segment[..bracket], index));
        }

        return segments.Count > 0;
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
