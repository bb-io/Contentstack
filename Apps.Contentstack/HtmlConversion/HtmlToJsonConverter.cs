using System.Web;
using Apps.Contentstack.HtmlConversion.Constants;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public class HtmlToJsonConverter
{
    public static void UpdateEntryFromHtml(Stream file, JObject entry)
    {
        var doc = new HtmlDocument();
        doc.Load(file);

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
}