using System.Text;
using Apps.Contentstack.Extensions;
using Apps.Contentstack.HtmlConversion.Constants;
using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class JsonToHtmlConverter
{
    public static byte[] ToHtml(JObject entry, ContentTypeBlockEntity contentType)
    {
        try
        {
            var (doc, body) = PrepareEmptyHtmlDocument();

            ParseEntryToHtml(entry, contentType, doc, body);

            return Encoding.UTF8.GetBytes(doc.DocumentNode.OuterHtml);
        }
        catch(Exception ex)
        {
            throw new($"Conversion to HTML failed. Entry json: {entry}; Content type schema: {contentType.Schema}; Exception: {ex}");
        }
    }

    private static void ParseEntryToHtml(JObject entry, ContentTypeBlockEntity contentType, HtmlDocument doc,
        HtmlNode body)
    {
        contentType.Schema.GetLocalizableFields().ForEach(x =>
        {
            var property = entry[x.Uid];

            switch (x.DataType)
            {
                case "json":
                    JsonRichTextToHtml(doc, body, (property as JObject)!);
                    break;
                case "blocks":
                    BlocksToHtml(doc, body, (property as JArray)!, x);
                    break;
                case "global_field":
                    GlobalFieldToHtml(doc, body, (property as JObject)!, x);
                    break;
            }

            if (property?.Type != JTokenType.String)
                return;

            AppendContent(doc, body, property, HtmlConstants.Div);
        });
    }

    private static void BlocksToHtml(HtmlDocument doc, HtmlNode body, JArray? blocks, EntryProperty entryProperty)
    {
        if (entryProperty.Blocks is null || blocks is null)
            return;

        blocks.ToList().ForEach(x =>
        {
            var block = x.First as JProperty;
            var blockName = block!.Name;

            var contentType = entryProperty.Blocks.First(x => x.Uid == blockName);
            ParseEntryToHtml((block.Value as JObject)!, contentType, doc, body);
        });
    }

    private static void JsonRichTextToHtml(HtmlDocument doc, HtmlNode body, JObject property)
    {
        var richTextNode = doc.CreateElement(HtmlConstants.Div);

        var contentNodes = property.Descendants()
            .Where(x => x is JProperty { Name: "text" })
            .OfType<JProperty>()
            .ToList();

        contentNodes.ForEach(x => AppendContent(doc, richTextNode, x, HtmlConstants.Span));
        body.AppendChild(richTextNode);
    }

    private static void GlobalFieldToHtml(HtmlDocument doc, HtmlNode body, JObject property, EntryProperty entryProperty)
    {
        ParseEntryToHtml(property, new()
        {
            Schema = entryProperty.Schema
        }, doc, body);
    }

    private static void AppendContent(HtmlDocument doc, HtmlNode parentNode, JToken property, string htmlTag)
    {
        var contentNode = doc.CreateElement(htmlTag);
        contentNode.SetAttributeValue(ConversionConstants.PathAttr, property.Path);

        contentNode.InnerHtml = property is JProperty jProperty ? jProperty.Value.ToString() : property.ToString();
        parentNode.AppendChild(contentNode);
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