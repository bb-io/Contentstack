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
    public static byte[] ToHtml(JObject entry, ContentTypeBlockEntity contentType, Logger? logger, string contentTypeId,
        string entryId)
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

            var max = x.ContentTypeSchema?["max"]?.Value<int>();
            
            if (x.Multiple && property is JArray propertyArray)
            {
                var containerNode = doc.CreateElement(HtmlConstants.Div);
                containerNode.SetAttributeValue(ConversionConstants.PathAttr, x.Uid);
                
                if (max.HasValue)
                {
                    containerNode.SetAttributeValue("max", max.Value.ToString());
                }
                
                // Create individual item containers for each array element
                foreach (var item in propertyArray)
                {
                    var itemContainer = doc.CreateElement("div");
                    
                    // For rich text, we preserve the HTML structure but wrap it in a container
                    if (x.DataType == "text" && 
                        item.ToString().StartsWith("<") && 
                        item.ToString().EndsWith(">"))
                    {
                        itemContainer.SetAttributeValue("class", ConversionConstants.MultipleItemClass);
                        itemContainer.InnerHtml = item.ToString();
                    }
                    else
                    {
                        itemContainer.SetAttributeValue("class", ConversionConstants.MultipleComplexItemClass);
                        ProcessPropertyByType(item, x, doc, itemContainer, max);
                    }
                    
                    containerNode.AppendChild(itemContainer);
                }
                
                body.AppendChild(containerNode);
                return;
            }
            
            ProcessPropertyByType(property, x, doc, body, max);
        });
    }
    
    private static void ProcessPropertyByType(JToken property, EntryProperty entryProperty, HtmlDocument doc, 
        HtmlNode parentNode, int? max = null)
    {
        if (property == null)
            return;
            
        switch (entryProperty.DataType)
        {
            case "json":
                JsonRichTextToHtml(doc, parentNode, property as JObject, max);
                break;
            case "blocks":
                BlocksToHtml(doc, parentNode, property as JArray, entryProperty);
                break;
            case "global_field":
                GlobalFieldToHtml(doc, parentNode, property as JObject, entryProperty, max);
                break;
            case "link":
                LinkToHtml(doc, parentNode, property as JObject, entryProperty, max);
                break;
            case "group" when entryProperty.Uid == "comments":
                CommentsToHtml(doc, parentNode, property as JObject, entryProperty, max);
                break;
            case "group":
                GroupToHtml(doc, parentNode, property as JObject, entryProperty, max);
                break;
            default:
                if (property.Type == JTokenType.String)
                {
                    // Handle rich text content in string format
                    string stringValue = property.ToString();
                    if (stringValue.StartsWith("<") && stringValue.EndsWith(">"))
                    {
                        var contentNode = doc.CreateElement(HtmlConstants.Div);
                        contentNode.SetAttributeValue(ConversionConstants.PathAttr, property.Path);
                        
                        if (max.HasValue)
                        {
                            contentNode.SetAttributeValue("max", max.Value.ToString());
                        }
                        
                        contentNode.InnerHtml = stringValue;
                        parentNode.AppendChild(contentNode);
                    }
                    else
                    {
                        AppendContent(doc, parentNode, property, HtmlConstants.Div, max);
                    }
                }
                break;
        }
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

    private static void JsonRichTextToHtml(HtmlDocument doc, HtmlNode body, JObject? property, int? max = null)
    {
        if (property is null)
            return;

        var richTextNode = doc.CreateElement(HtmlConstants.Div);

        var contentNodes = property.Descendants()
            .Where(x => x is JProperty { Name: "text" })
            .OfType<JProperty>()
            .ToList();

        contentNodes.ForEach(x => AppendContent(doc, richTextNode, x, HtmlConstants.Span, max));
        body.AppendChild(richTextNode);
    }

    private static void GlobalFieldToHtml(HtmlDocument doc, HtmlNode body, JObject? property, EntryProperty entryProperty, int? max = null)
    {
        if (property is null || entryProperty.Schema is null)
            return;

        ParseEntryToHtml(property, new()
        {
            Schema = entryProperty.Schema
        }, doc, body);
    }

    private static void LinkToHtml(HtmlDocument doc, HtmlNode body, JObject? property, EntryProperty entryProperty, int? max = null)
    {
        if (property is null)
            return;

        AppendContent(doc, body, property["title"]!, HtmlConstants.Div, max);
        AppendContent(doc, body, property["href"]!, HtmlConstants.Div, max);
    }

    private static void AppendContent(HtmlDocument doc, HtmlNode parentNode, JToken property, string htmlTag,
        int? max = null)
    {
        var contentNode = doc.CreateElement(htmlTag);
        contentNode.SetAttributeValue(ConversionConstants.PathAttr, property.Path);

        if (max.HasValue)
        {
            contentNode.SetAttributeValue("max", max.Value.ToString());
        }

        contentNode.InnerHtml = property is JProperty jProperty ? jProperty.Value.ToString() : property.ToString();
        parentNode.AppendChild(contentNode);
    }


    private static (HtmlDocument document, HtmlNode bodyNode) PrepareEmptyHtmlDocument(string contentTypeId,
        string entryId)
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

    private static void CommentsToHtml(HtmlDocument doc, HtmlNode body, JObject? property, EntryProperty entryProperty, int? max = null)
    {
        if(property is null)
            return;
    
        AppendContent(doc, body, property["comment"]!, HtmlConstants.Div, max);
        LinkToHtml(doc, body, property["call_to_action"] as JObject, entryProperty, max);
    }

    private static void GroupToHtml(HtmlDocument doc, HtmlNode parentNode, JObject? groupProperty, EntryProperty entryProperty, int? max = null)
    {
        if (groupProperty is null || entryProperty.Schema is null)
            return;
            
        var groupContainer = doc.CreateElement(HtmlConstants.Div);
        groupContainer.SetAttributeValue(ConversionConstants.PathAttr, entryProperty.Uid);
        
        if (max.HasValue)
        {
            groupContainer.SetAttributeValue("max", max.Value.ToString());
        }
        
        // Process each property in the group
        foreach (var property in groupProperty.Properties())
        {
            // Skip metadata and other special properties
            if (property.Name.StartsWith("_"))
                continue;
                
            // Find the schema for this property
            var propertySchema = entryProperty.Schema.FirstOrDefault(s => s["uid"]?.ToString() == property.Name);
            if (propertySchema != null)
            {
                // Create entry property for nested field
                var nestedProperty = new EntryProperty
                {
                    Uid = property.Name,
                    DataType = propertySchema["data_type"]?.ToString()
                };
                
                // Process the nested property
                ProcessPropertyByType(property.Value, nestedProperty, doc, groupContainer);
            }
            else
            {
                // Fallback for properties without schema
                AppendContent(doc, groupContainer, property.Value, HtmlConstants.Div);
            }
        }
        
        parentNode.AppendChild(groupContainer);
    }
}