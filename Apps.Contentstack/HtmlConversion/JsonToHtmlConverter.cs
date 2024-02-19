using Apps.Contentstack.HtmlConversion.Constants;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class JsonToHtmlConverter
{
    public static byte[] ToHtml(JObject entry)
    {
        var (doc, body) = PrepareEmptyHtmlDocument();
        
        throw new NotImplementedException();
    }
    
    private static (HtmlDocument document, HtmlNode bodyNode) PrepareEmptyHtmlDocument()
    {
        var htmlDoc = new HtmlDocument();
        var htmlNode = htmlDoc.CreateElement(HtmlConstants.Html);
        htmlDoc.DocumentNode.AppendChild(htmlNode);
        htmlNode.AppendChild(htmlDoc.CreateElement(HtmlConstants.Head));
    
        var bodyNode = htmlDoc.CreateElement(HtmlConstants.Body);
        htmlNode.AppendChild(bodyNode);
    
        return (htmlDoc, bodyNode);
    }
}