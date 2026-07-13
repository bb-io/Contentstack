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

    public static void UpdateEntryFromHtml(Stream file, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);

        try
        {
            ApplyHtmlToEntry(doc, entry, logger);
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

    public static void UpdateReferencedEntryFromHtml(Stream file, string contentTypeId, string entryId, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file, System.Text.Encoding.UTF8);
        file.Position = 0;

        var articleNode = doc.DocumentNode.SelectSingleNode(
            $"//article[@{ConversionConstants.RefContentTypeAttr}='{contentTypeId}' and @{ConversionConstants.RefEntryIdAttr}='{entryId}']");

        if (articleNode is null)
            return;

        var tempDoc = new HtmlDocument();
        tempDoc.LoadHtml($"<body>{articleNode.InnerHtml}</body>");

        try
        {
            ApplyHtmlToEntry(tempDoc, entry, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError.Invoke($"Failed to update referenced entry {entryId}: {ex}", null);
            throw;
        }
    }

    private static void ApplyHtmlToEntry(HtmlDocument doc, JObject entry, Logger? logger)
    {
        var entryNodes = doc.DocumentNode.Descendants()
            .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null &&
                        !x.Ancestors().Any(a => a.Name == "article"))
            .ToList();

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
                var itemValue = HttpUtility.HtmlDecode(multipleItems[i].InnerHtml.Trim());
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
            {
                var innerHtml = x.Name == "span" ? x.InnerHtml : x.InnerHtml.Trim();
                jValue.Value = HttpUtility.HtmlDecode(innerHtml);
            }
        });
    }

    private static void SetFileUidAtPath(JObject entry, string path, string uid)
    {
        var existing = entry.SelectToken(path);
        if (existing == null)
            return;

        existing.Replace(new JValue(uid));
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
