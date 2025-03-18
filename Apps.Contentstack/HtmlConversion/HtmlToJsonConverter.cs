using System.Web;
using Apps.Contentstack.HtmlConversion.Constants;
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
            // First handle simple repeatable fields
            var repeatableNodes = doc.DocumentNode.Descendants()
                .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null && 
                       x.SelectNodes($"./div[@class='{ConversionConstants.MultipleItemClass}']") != null &&
                       x.SelectNodes($"./div[@class='{ConversionConstants.MultipleItemClass}']").Count > 0)
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
                
                // Make sure we have the right number of items
                if (multipleItems.Count != arrayToken.Count)
                {
                    logger?.LogWarning.Invoke($"Mismatch in array lengths for path {path}. HTML has {multipleItems.Count} items, JSON has {arrayToken.Count} items", null);
                }

                // Update each item in the array
                for (int i = 0; i < Math.Min(multipleItems.Count, arrayToken.Count); i++)
                {
                    var itemValue = HttpUtility.HtmlDecode(multipleItems[i].InnerHtml.Trim());
                    if (arrayToken[i] is JValue jValue)
                    {
                        jValue.Value = itemValue;
                    }
                    else
                    {
                        arrayToken[i] = itemValue;
                    }
                }
            }

            // Then handle all direct path mappings (including complex objects)
            var localizableNodes = doc.DocumentNode.Descendants()
                .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null)
                .ToList();

            localizableNodes.ForEach(x =>
            {
                var path = x.Attributes[ConversionConstants.PathAttr].Value!;
                var propertyValue = entry.SelectToken(path);
                if (propertyValue == null)
                {
                    return;
                }

                if (propertyValue is JValue jValue)
                {
                    jValue.Value = HttpUtility.HtmlDecode(x.InnerHtml.Trim());
                }
            });
        }
        catch(Exception ex)
        {
            logger?.LogError.Invoke($"Conversion to Contentstack JSON failed. Entry json: {entry}; HTML: {doc.DocumentNode.OuterHtml}; Exception: {ex}", null);
            throw new("The HTML file structure should match the source article");
        }
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
