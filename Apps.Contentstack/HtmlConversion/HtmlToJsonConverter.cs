using System.Web;
using Apps.Contentstack.HtmlConversion.Constants;
using Blackbird.Applications.Sdk.Common.Invocation;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public class HtmlToJsonConverter
{
    public static void UpdateEntryFromHtml(Stream file, JObject entry, Logger? logger)
    {
        var doc = new HtmlDocument();
        doc.Load(file);

        try
        {
            var localizableNodes = doc.DocumentNode.Descendants()
                .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null)
                .ToList();

            localizableNodes.ForEach(x =>
            {
                var path = x.Attributes[ConversionConstants.PathAttr].Value!;
                var propertyValue = entry.SelectToken(path);

                (propertyValue as JValue)!.Value = HttpUtility.HtmlDecode(x.InnerHtml.Trim());
            });
        }
        catch(Exception ex)
        {
            logger?.LogError.Invoke($"Conversion to Contentstack JSON failed. Entry json: {entry}; HTML: {doc.DocumentNode.OuterHtml}; Exception: {ex}", null);
            throw new("The HTML file structure should match the source article");
        }
    }
}