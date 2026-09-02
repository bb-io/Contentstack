using System.Text;
using Apps.Contentstack.Extensions;
using Apps.Contentstack.HtmlConversion.Constants;
using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Entities;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Filters.Shared;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class JsonToHtmlConverter
{
    public static byte[] ToHtml(
        JObject entry,
        ContentTypeBlockEntity contentType,
        Logger? logger,
        string contentTypeId,
        string entryId,
        string stackApiKey,
        UserEntity? updatedByUser,
        IEnumerable<string>? excludedFieldIds = null,
        IEnumerable<ReferencedEntryData>? referencedEntries = null)
    {
        try
        {
            var (doc, body) = PrepareEmptyHtmlDocument(contentTypeId, entryId, entry, stackApiKey, updatedByUser);
            var excludedFields = excludedFieldIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            ParseEntryToHtml(entryId, entry, contentType, doc, body, excludedFields);

            if (referencedEntries != null)
            {
                foreach (var refData in referencedEntries)
                {
                    var articleNode = doc.CreateElement("article");
                    articleNode.SetAttributeValue(ConversionConstants.RefContentTypeAttr, refData.ContentTypeId);
                    articleNode.SetAttributeValue(ConversionConstants.RefEntryIdAttr, refData.EntryId);
                    ParseEntryToHtml(refData.EntryId, refData.Entry, refData.Schema, doc, articleNode, excludedFields);
                    body.AppendChild(articleNode);
                }
            }

            KeepKeysOnContentUnitsOnly(doc.DocumentNode);

            return Encoding.UTF8.GetBytes(doc.DocumentNode.OuterHtml);
        }
        catch (Exception ex)
        {
            logger?.LogError.Invoke(
                $"Conversion to HTML failed. Entry json: {entry}; Content type schema: {contentType.Schema}; Exception: {ex}",
                null);
            throw new PluginApplicationException("Conversion to HTML failed.");
        }
    }

    private static void ParseEntryToHtml(string entryId, JObject entry, ContentTypeBlockEntity contentType,
        HtmlDocument doc,
        HtmlNode body, ISet<string> excludedFieldIds)
    {
        contentType.Schema.GetLocalizableFields().ForEach(x =>
        {
            if (excludedFieldIds.Contains(x.Uid))
                return;

            var property = entry[x.Uid];

            if (property is null)
                return;

            var max = x.ContentTypeSchema?["max"]?.Value<int>();

            if (x.Multiple && property is JArray propertyArray)
            {
                var containerNode = doc.CreateElement(HtmlConstants.Div);
                SetPathAndKey(containerNode, entryId, ResolvePath(property, x.Uid));

                ApplyMax(containerNode, max);

                foreach (var item in propertyArray)
                {
                    var itemContainer = doc.CreateElement("div");

                    if (x.DataType == "text" && IsHtmlValue(x, item.ToString()))
                    {
                        itemContainer.SetAttributeValue("class", ConversionConstants.MultipleItemClass);
                        itemContainer.SetAttributeValue(ConversionConstants.BlackbirdFieldType,
                            ConversionConstants.HtmlFieldType);
                        SetKey(itemContainer, entryId, item.Path);
                        itemContainer.InnerHtml = item.ToString();
                    }
                    else
                    {
                        itemContainer.SetAttributeValue("class", ConversionConstants.MultipleComplexItemClass);
                        ProcessPropertyByType(entryId, item, x, doc, itemContainer, excludedFieldIds, max);
                    }

                    containerNode.AppendChild(itemContainer);
                }

                body.AppendChild(containerNode);
                return;
            }

            ProcessPropertyByType(entryId, property, x, doc, body, excludedFieldIds, max);
        });
    }

    private static void ProcessPropertyByType(string entryId, JToken property, EntryProperty entryProperty,
        HtmlDocument doc,
        HtmlNode parentNode, ISet<string> excludedFieldIds, int? max = null)
    {
        if (property == null)
            return;

        switch (entryProperty.DataType)
        {
            case "json":
                JsonRichTextToHtml(entryId, doc, parentNode, property as JObject, max);
                break;
            case "blocks":
                if (property is JArray jArray)
                    BlocksToHtml(entryId, doc, parentNode, jArray, entryProperty, excludedFieldIds);
                else if (property is JObject jObject)
                {
                    var block = jObject.First as JProperty;
                    if (block != null && entryProperty.Blocks != null)
                    {
                        var blockName = block.Name;
                        var blockContentType = entryProperty.Blocks.FirstOrDefault(b => b.Uid == blockName);

                        if (blockContentType != null)
                            ParseEntryToHtml(entryId, (block.Value as JObject)!, blockContentType, doc, parentNode,
                                excludedFieldIds);
                    }
                }

                break;
            case "global_field":
                GlobalFieldToHtml(entryId, doc, parentNode, property as JObject, entryProperty, excludedFieldIds, max);
                break;
            case "link":
                LinkToHtml(entryId, doc, parentNode, property as JObject, entryProperty, max);
                break;
            case "file":
                FileToHtml(entryId, doc, parentNode, property, entryProperty);
                break;
            case "group" when entryProperty.Uid == "comments":
                CommentsToHtml(entryId, doc, parentNode, property as JObject, entryProperty, max);
                break;
            case "group":
                GroupToHtml(entryId, doc, parentNode, property as JObject, entryProperty, excludedFieldIds, max);
                break;
            default:
                if (property.Type == JTokenType.String)
                    StringToHtml(entryId, doc, parentNode, property, entryProperty, max);

                break;
        }
    }

    /// <summary>
    /// A string field is either plain text or an HTML rich text value. The schema decides: guessing from the
    /// value's shape misses markup that starts or ends with a bare text run, and escaping such a value turns
    /// its tables and paragraphs into visible source.
    /// </summary>
    private static void StringToHtml(string entryId, HtmlDocument doc, HtmlNode parentNode, JToken property,
        EntryProperty entryProperty, int? max)
    {
        var value = property.ToString();

        if (!IsHtmlValue(entryProperty, value))
        {
            AppendContent(entryId, doc, parentNode, property, HtmlConstants.Div, max);
            return;
        }

        var contentNode = doc.CreateElement(HtmlConstants.Div);
        SetPathAndKey(contentNode, entryId, ResolvePath(property, entryProperty.Uid));
        contentNode.SetAttributeValue(ConversionConstants.BlackbirdFieldType, ConversionConstants.HtmlFieldType);
        ApplyMax(contentNode, max);

        contentNode.InnerHtml = value;
        parentNode.AppendChild(contentNode);
    }

    // The shape check stays as a fallback for schemas that carry no field metadata.
    private static bool IsHtmlValue(EntryProperty entryProperty, string value)
        => entryProperty.IsHtmlRichText || (value.StartsWith('<') && value.EndsWith('>'));

    private static void SetPathAndKey(HtmlNode node, string entryId, string path)
    {
        SetPath(node, path);
        SetKey(node, entryId, path);
    }

    private static void SetPath(HtmlNode node, string path)
        => node.SetAttributeValue(ConversionConstants.PathAttr, path);

    internal static void SetKey(HtmlNode node, string entryId, string path)
        => node.SetAttributeValue(ConversionConstants.BlackbirdKey, $"{entryId}-{path}");

    private static bool HasKey(HtmlNode node)
        => node.Attributes[ConversionConstants.BlackbirdKey] is not null;

    private static void KeepKeysOnContentUnitsOnly(HtmlNode root)
    {
        root.DescendantsAndSelf()
            .Where(x => HasKey(x) && x.Descendants().Any(HasKey))
            .ToList()
            .ForEach(x => x.Attributes.Remove(ConversionConstants.BlackbirdKey));
    }

    private static string ResolvePath(JToken token, string uid)
        => string.IsNullOrEmpty(token.Path) ? uid : token.Path;

    private static void ApplyMax(HtmlNode node, int? max)
    {
        if (!max.HasValue)
            return;

        node.SetAttributeValue("max", max.Value.ToString());
        var serialized = SizeRestrictionHelper.Serialize(new SizeRestrictions { MaximumSize = max.Value });
        node.SetAttributeValue(ConversionConstants.BlackbirdMax, serialized!);
    }

    private static void BlocksToHtml(string entryId, HtmlDocument doc, HtmlNode body, JArray? blocks,
        EntryProperty entryProperty,
        ISet<string> excludedFieldIds)
    {
        if (entryProperty.Blocks is null || blocks is null)
            return;

        blocks.ToList().ForEach(x =>
        {
            var block = x.First as JProperty;
            var blockName = block!.Name;

            var contentType = entryProperty.Blocks.First(x => x.Uid == blockName);
            ParseEntryToHtml(entryId, (block.Value as JObject)!, contentType, doc, body, excludedFieldIds);
        });
    }

    private static void JsonRichTextToHtml(string entryId, HtmlDocument doc, HtmlNode body, JObject? property,
        int? max = null)
    {
        if (property is null)
            return;

        var richTextNode = doc.CreateElement(HtmlConstants.Div);

        if (!string.IsNullOrEmpty(property.Path))
        {
            SetPathAndKey(richTextNode, entryId, property.Path);
            richTextNode.SetAttributeValue(ConversionConstants.BlackbirdJsonValue, EncodeJsonValue(property));
        }

        RichTextRenderer.RenderChildren(entryId, doc, richTextNode, property, max);
        body.AppendChild(richTextNode);
    }

    internal static string EncodeJsonValue(JToken value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value.ToString(Formatting.None)));

    private static void GlobalFieldToHtml(string entryId, HtmlDocument doc, HtmlNode body, JObject? property,
        EntryProperty entryProperty,
        ISet<string> excludedFieldIds, int? max = null)
    {
        if (property is null || entryProperty.Schema is null)
            return;

        ParseEntryToHtml(entryId, property, new()
        {
            Schema = entryProperty.Schema
        }, doc, body, excludedFieldIds);
    }

    private static void LinkToHtml(string entryId, HtmlDocument doc, HtmlNode body, JObject? property,
        EntryProperty entryProperty, int? max = null)
    {
        if (property is null)
            return;

        AppendContent(entryId, doc, body, property["title"]!, HtmlConstants.Div, max);
        AppendContent(entryId, doc, body, property["href"]!, HtmlConstants.Div, max);
    }

    private static void FileToHtml(string entryId, HtmlDocument doc, HtmlNode parentNode, JToken? property,
        EntryProperty entryProperty)
    {
        if (property is null || property.Type == JTokenType.Null)
            return;

        var uid = ExtractAssetUid(property);
        if (string.IsNullOrEmpty(uid))
            return;

        var contentNode = doc.CreateElement(HtmlConstants.Div);
        SetPathAndKey(contentNode, entryId, ResolvePath(property, entryProperty.Uid));
        contentNode.SetAttributeValue(ConversionConstants.BlackbirdFieldType, ConversionConstants.FileFieldType);
        contentNode.SetAttributeValue(ConversionConstants.BlackbirdFileUid, uid);
        parentNode.AppendChild(contentNode);
    }

    private static string? ExtractAssetUid(JToken token)
    {
        return token switch
        {
            JValue { Value: string s } => s,
            JObject obj => obj["uid"]?.ToString(),
            _ => null
        };
    }

    internal static void AppendContent(string entryId, HtmlDocument doc, HtmlNode parentNode, JToken property,
        string htmlTag,
        int? max = null, string? fieldType = null)
    {
        var contentNode = doc.CreateElement(htmlTag);
        SetPath(contentNode, property.Path);

        if (fieldType is not null)
            contentNode.SetAttributeValue(ConversionConstants.BlackbirdFieldType, fieldType);

        if (fieldType != ConversionConstants.RichTextNodeFieldType)
            SetKey(contentNode, entryId, property.Path);

        if (max.HasValue)
        {
            contentNode.SetAttributeValue("max", max.Value.ToString());
            var serialized = SizeRestrictionHelper.Serialize(new SizeRestrictions { MaximumSize = max.Value });
            contentNode.SetAttributeValue(ConversionConstants.BlackbirdMax, serialized!);
        }

        var value = property is JProperty jProperty ? jProperty.Value.ToString() : property.ToString();
        contentNode.InnerHtml = EncodeText(value);
        parentNode.AppendChild(contentNode);
    }

    /// <summary>
    /// These values are plain text, not markup. Escaping the three text-node metacharacters keeps a stray
    /// '&lt;' or '&amp;' from breaking the document; the upload side reverses it with HtmlDecode.
    /// </summary>
    private static string EncodeText(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static void AddBlackbirdMeta(HtmlDocument htmlDoc, HtmlNode headNode, string name, string? value)
    {
        if (value is null) return;
        var entryMetaNode = htmlDoc.CreateElement("meta");
        entryMetaNode.SetAttributeValue("name", $"blackbird-{name}");
        entryMetaNode.SetAttributeValue("content", value);
        headNode.AppendChild(entryMetaNode);
    }

    private static (HtmlDocument document, HtmlNode bodyNode) PrepareEmptyHtmlDocument(string contentTypeId,
        string entryId, JObject entry, string stackApiKey, UserEntity? updatedByUser)
    {
        var locale = entry["locale"]?.Value<string>() ?? "en-us";
        var title = entry["title"]?.Value<string>();

        var htmlDoc = new HtmlDocument();
        var htmlNode = htmlDoc.CreateElement(HtmlConstants.Html);
        htmlNode.SetAttributeValue("lang", locale);
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

        AddBlackbirdMeta(htmlDoc, headNode, "ucid", entryId);
        AddBlackbirdMeta(htmlDoc, headNode, "content-name", title);
        AddBlackbirdMeta(htmlDoc, headNode, "admin-url",
            $"https://app.contentstack.com/#!/stack/{stackApiKey}/content-type/{contentTypeId}/{locale}/entry/{entryId}/edit");
        AddBlackbirdMeta(htmlDoc, headNode, "system-name", "Contentstack");
        AddBlackbirdMeta(htmlDoc, headNode, "system-ref", "https://www.contentstack.com");

        var bodyNode = htmlDoc.CreateElement(HtmlConstants.Body);

        bodyNode.SetAttributeValue("its-rev-tool", "Contentstack");
        bodyNode.SetAttributeValue("its-rev-tool-ref", "https://www.contentstack.com");

        if (updatedByUser is not null)
        {
            bodyNode.SetAttributeValue("its-rev-person", $"{updatedByUser.FirstName} {updatedByUser.LastName}");
        }

        htmlNode.AppendChild(bodyNode);

        return (htmlDoc, bodyNode);
    }

    private static void CommentsToHtml(string entryId, HtmlDocument doc, HtmlNode body, JObject? property,
        EntryProperty entryProperty, int? max = null)
    {
        if (property is null)
            return;

        AppendContent(entryId, doc, body, property["comment"]!, HtmlConstants.Div, max);
        LinkToHtml(entryId, doc, body, property["call_to_action"] as JObject, entryProperty, max);
    }

    private static void GroupToHtml(string entryId, HtmlDocument doc, HtmlNode parentNode, JObject? groupProperty,
        EntryProperty entryProperty,
        ISet<string> excludedFieldIds, int? max = null)
    {
        if (groupProperty is null || entryProperty.Schema is null)
            return;

        var groupContainer = doc.CreateElement(HtmlConstants.Div);
        SetPathAndKey(groupContainer, entryId, ResolvePath(groupProperty, entryProperty.Uid));

        if (max.HasValue)
        {
            groupContainer.SetAttributeValue("max", max.Value.ToString());
            var serialized = SizeRestrictionHelper.Serialize(new SizeRestrictions { MaximumSize = max.Value });
            groupContainer.SetAttributeValue(ConversionConstants.BlackbirdMax, serialized!);
        }

        foreach (var property in groupProperty.Properties())
        {
            if (property.Name.StartsWith("_"))
                continue;

            if (excludedFieldIds.Contains(property.Name))
                continue;

            var propertySchema = entryProperty.Schema.FirstOrDefault(s => s["uid"]?.ToString() == property.Name);
            if (propertySchema != null)
            {
                var nestedProperty = propertySchema.ToObject<EntryProperty>()!;
                nestedProperty.Uid = property.Name;

                if (nestedProperty.Multiple && property.Value is JArray arrayValue)
                {
                    foreach (var item in arrayValue)
                    {
                        var itemContainer = doc.CreateElement(HtmlConstants.Div);
                        itemContainer.SetAttributeValue("class", ConversionConstants.MultipleComplexItemClass);
                        ProcessPropertyByType(entryId, item, nestedProperty, doc, itemContainer, excludedFieldIds);
                        groupContainer.AppendChild(itemContainer);
                    }
                }
                else
                {
                    ProcessPropertyByType(entryId, property.Value, nestedProperty, doc, groupContainer,
                        excludedFieldIds);
                }
            }
            else
            {
                AppendContent(entryId, doc, groupContainer, property.Value, HtmlConstants.Div);
            }
        }

        parentNode.AppendChild(groupContainer);
    }
}
