using System.Text;
using Apps.Contentstack.HtmlConversion.Constants;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Extensions;

public static class HtmlNodeExtensions
{
    public static bool HasKey(this HtmlNode node) => node.Attributes[ConversionConstants.BlackbirdKey] is not null;

    public static bool IsInlineEmbed(this HtmlNode node) => node.Name == HtmlConstants.Span && node.HasKey();

    public static bool IsTextMarker(this HtmlNode node)
    {
        return node.Attributes[ConversionConstants.PathAttr] is not null ||
               node.Attributes[ConversionConstants.BlackbirdTextIndex] is not null;
    }
    
    public static string GetParagraphPath(this HtmlNode paragraph)
    {
        string key = paragraph.Attributes[ConversionConstants.BlackbirdKey].Value;
        return key[(key.IndexOf('-') + 1)..];
    }
    
    public static bool IsTextSpan(this HtmlNode node)
    {
        return node.Attributes[ConversionConstants.BlackbirdFieldType]?.Value == ConversionConstants.RichTextNodeFieldType;
    }
    
    public static bool IsJsonRichText(this HtmlNode node)
    {
        return node.Attributes[ConversionConstants.BlackbirdJsonValue] is not null &&
               node.Attributes[ConversionConstants.PathAttr] is not null;
    }

    public static JToken? FieldValue(this HtmlNode container)
    {
        try
        {
            var encoded = container.Attributes[ConversionConstants.BlackbirdJsonValue].Value;
            return JToken.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    public static string? GetPath(this HtmlNode node) => node.Attributes[ConversionConstants.PathAttr]?.Value;
}