using Apps.Contentstack.HtmlConversion.Constants;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

internal static class RichTextRenderer
{
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "pre", "code", "hr", "br",
        "ol", "ul", "li", "table", "thead", "tbody", "tfoot", "tr", "th", "td",
        "a", "img", "figure", "figcaption", "details", "summary", "span", "div"
    };

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img"
    };

    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "span", "code"
    };

    private static readonly (string Mark, string Tag)[] TextMarks =
    [
        ("bold", "strong"),
        ("italic", "em"),
        ("underline", "u"),
        ("strikethrough", "s"),
        ("superscript", "sup"),
        ("subscript", "sub"),
        ("inlineCode", "code")
    ];

    public static void RenderChildren(string entryId, HtmlDocument doc, HtmlNode parent, JObject node, int? max)
        => Render(entryId, doc, parent, node["children"], max);

    private static void Render(string entryId, HtmlDocument doc, HtmlNode parent, JToken? node, int? max)
    {
        switch (node)
        {
            case JArray array:
                foreach (var child in array)
                    Render(entryId, doc, parent, child, max);
                break;
            case JObject obj when obj.Property("text") is not null:
                RenderText(entryId, doc, parent, obj, max);
                break;
            case JObject obj:
                RenderElement(entryId, doc, parent, obj, max);
                break;
        }
    }

    private static void RenderElement(string entryId, HtmlDocument doc, HtmlNode parent, JObject node, int? max)
    {
        var tag = ResolveTag(node["type"]?.Value<string>());
        var element = doc.CreateElement(tag);

        ApplyAttributes(element, tag, node["attrs"] as JObject);

        if (!InlineTags.Contains(tag) && !VoidTags.Contains(tag) && !string.IsNullOrEmpty(node.Path))
            JsonToHtmlConverter.SetKey(element, entryId, node.Path);

        parent.AppendChild(element);

        Render(entryId, doc, VoidTags.Contains(tag) ? parent : element, node["children"], max);
    }

    private static void RenderText(string entryId, HtmlDocument doc, HtmlNode parent, JObject node, int? max)
    {
        var textProperty = node.Property("text")!;
        var text = textProperty.Value.ToString();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            if (text.Length > 0)
                parent.AppendChild(doc.CreateTextNode(text));

            return;
        }

        var target = parent;

        foreach (var (mark, tag) in TextMarks)
        {
            if (node[mark]?.Type != JTokenType.Boolean || !node[mark]!.Value<bool>())
                continue;

            var wrapper = doc.CreateElement(tag);
            target.AppendChild(wrapper);
            target = wrapper;
        }

        JsonToHtmlConverter.AppendContent(entryId, doc, target, textProperty, HtmlConstants.Span, max,
            ConversionConstants.RichTextNodeFieldType);
    }

    private static string ResolveTag(string? type)
        => type is not null && KnownTags.Contains(type) ? type.ToLowerInvariant() : HtmlConstants.Div;

    private static void ApplyAttributes(HtmlNode element, string tag, JObject? attrs)
    {
        if (attrs is null)
            return;

        switch (tag)
        {
            case HtmlConstants.Anchor:
                SetIfPresent(element, "href", attrs["url"]);
                SetIfPresent(element, "target", attrs["target"]);
                break;
            case HtmlConstants.Image:
                SetIfPresent(element, "src", attrs["url"] ?? attrs["asset-link"] ?? attrs["src"]);
                SetIfPresent(element, "alt", attrs["alt"] ?? attrs["asset-name"]);
                break;
            case HtmlConstants.TableCell:
            case HtmlConstants.TableHeaderCell:
                SetIfPresent(element, "colspan", attrs["colSpan"] ?? attrs["colspan"]);
                SetIfPresent(element, "rowspan", attrs["rowSpan"] ?? attrs["rowspan"]);
                break;
        }
    }

    private static void SetIfPresent(HtmlNode element, string name, JToken? value)
    {
        if (value is null || value.Type is JTokenType.Null or JTokenType.Object or JTokenType.Array)
            return;

        var text = value.ToString();
        if (!string.IsNullOrEmpty(text))
            element.SetAttributeValue(name, text);
    }
}
