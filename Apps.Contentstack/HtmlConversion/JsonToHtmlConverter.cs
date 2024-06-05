using System.Text;
using Apps.Contentstack.Extensions;
using Apps.Contentstack.HtmlConversion.Constants;
using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Entities;
using Blackbird.Applications.Sdk.Common.Invocation;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class JsonToHtmlConverter
{
    public static byte[] ToHtml(JObject entry, ContentTypeBlockEntity contentType, Logger? logger, string contentTypeId, string entryId)
    {
        try
        {
            var (doc, body) = PrepareEmptyHtmlDocument(contentTypeId, entryId);

            ParseEntryToHtml(entry, contentType, doc, body);

            return Encoding.UTF8.GetBytes(doc.DocumentNode.OuterHtml);
        }
        catch (Exception ex)
        {
            logger?.LogError.Invoke(
                $"Conversion to HTML failed. Entry json: {entry}; Content type schema: {contentType.Schema}; Exception: {ex}",
                null);
            throw new("Conversion to HTML failed.");
        }
    }

    private static void ParseEntryToHtml(JObject entry, ContentTypeBlockEntity contentType, HtmlDocument doc,
        HtmlNode body)
    {
        contentType.Schema.GetLocalizableFields().ForEach(x =>
        {
            var property = entry[x.Uid];

            if (property is null)
                return;

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
                case "link":
                    LinkToHtml(doc, body, property as JObject, x);
                    break;
                case "group" when x.Uid == "comments":
                    CommentsToHtml(doc, body, property as JObject, x);
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

    private static void JsonRichTextToHtml(HtmlDocument doc, HtmlNode body, JObject? property)
    {
        if (property is null)
            return;

        var richTextNode = doc.CreateElement(HtmlConstants.Div);

        var contentNodes = property.Descendants()
            .Where(x => x is JProperty { Name: "text" })
            .OfType<JProperty>()
            .ToList();

        contentNodes.ForEach(x => AppendContent(doc, richTextNode, x, HtmlConstants.Span));
        body.AppendChild(richTextNode);
    }

    private static void GlobalFieldToHtml(HtmlDocument doc, HtmlNode body, JObject? property,
        EntryProperty entryProperty)
    {
        if (property is null || entryProperty.Schema is null)
            return;

        ParseEntryToHtml(property, new()
        {
            Schema = entryProperty.Schema
        }, doc, body);
    }

    private static void LinkToHtml(HtmlDocument doc, HtmlNode body, JObject? property, EntryProperty entryProperty)
    {
        if (property is null)
            return;

        AppendContent(doc, body, property["title"]!, HtmlConstants.Div);
        AppendContent(doc, body, property["href"]!, HtmlConstants.Div);
    }

    private static void AppendContent(HtmlDocument doc, HtmlNode parentNode, JToken property, string htmlTag)
    {
        var contentNode = doc.CreateElement(htmlTag);
        contentNode.SetAttributeValue(ConversionConstants.PathAttr, property.Path);

        contentNode.InnerHtml = property is JProperty jProperty ? jProperty.Value.ToString() : property.ToString();
        parentNode.AppendChild(contentNode);
    }

    private static (HtmlDocument document, HtmlNode bodyNode) PrepareEmptyHtmlDocument(string contentTypeId, string entryId)
    {
        var htmlDoc = new HtmlDocument();
        var htmlNode = htmlDoc.CreateElement(HtmlConstants.Html);
        htmlDoc.DocumentNode.AppendChild(htmlNode);
        
        var headNode = htmlDoc.CreateElement(HtmlConstants.Head);
        htmlNode.AppendChild(headNode);

        var metaContentTypeNode = htmlDoc.CreateElement("meta");
        metaContentTypeNode.SetAttributeValue("name", "blackbird-content-type-id");
        metaContentTypeNode.SetAttributeValue("content", contentTypeId);
        headNode.AppendChild(metaContentTypeNode);

        var metaEntryNode = htmlDoc.CreateElement("meta");
        metaEntryNode.SetAttributeValue("name", "blackbird-entry-id");
        metaEntryNode.SetAttributeValue("content", entryId);
        headNode.AppendChild(metaEntryNode);

        var bodyNode = htmlDoc.CreateElement(HtmlConstants.Body);
        htmlNode.AppendChild(bodyNode);

        return (htmlDoc, bodyNode);
    }

    private static void CommentsToHtml(HtmlDocument doc, HtmlNode body, JObject? property, EntryProperty entryProperty)
    {
        if(property is null)
            return;
        
        AppendContent(doc, body, property["comment"]!, HtmlConstants.Div);
        LinkToHtml(doc, body, property["call_to_action"] as JObject, entryProperty);
    }
}
