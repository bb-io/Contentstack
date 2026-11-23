using Apps.Contentstack.Extensions;
using Apps.Contentstack.HtmlConversion.Constants;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class RichTextSpanLocator
{
    public static void UseParagraphTextIndexes(HtmlNode root)
    {
        foreach (var container in root.Descendants().Where(x => x.IsJsonRichText()).ToList())
        {
            if (container.FieldValue() is not { } field)
                continue;

            string fieldPath = container.Attributes[ConversionConstants.PathAttr].Value;
            
            foreach (var paragraph in container.DescendantsAndSelf().Where(x => x.HasKey()).ToList())
            {
                var spans = paragraph.Descendants().Where(x => x.IsTextSpan()).ToList();
                var texts = VisibleTexts(field, fieldPath, paragraph.GetParagraphPath());

                if (spans.Count == 0 || spans.Count != texts.Count || spans.Zip(texts).Any(x => x.First.GetPath() != $"{fieldPath}.{x.Second.Path}"))
                    continue;

                for (var index = 0; index < spans.Count; index++)
                {
                    spans[index].Attributes.Remove(ConversionConstants.PathAttr);
                    spans[index].SetAttributeValue(ConversionConstants.BlackbirdTextIndex, index.ToString());
                }
            }
        }
    }

    public static (JValue? Target, string Label) Find(HtmlNode span, JToken field, string fieldPath)
    {
        var paragraph = span.Ancestors().FirstOrDefault(x => x.HasKey());

        if (span.Attributes[ConversionConstants.BlackbirdTextIndex] is { } indexAttribute)
        {
            string paragraphPath = paragraph is null ? fieldPath : paragraph.GetParagraphPath();
            string label = $"{paragraphPath} (text {indexAttribute.Value})";

            return int.TryParse(indexAttribute.Value, out var index) && paragraph is not null
                ? (TextAt(VisibleTexts(field, fieldPath, paragraphPath), index), label)
                : (null, label);
        }

        string path = span.GetPath()!;
        
        if (paragraph is not null && !IsUnder(path, paragraph.GetParagraphPath()) && CopiedTextIndex(span, path) is { } copiedIndex)
            return (TextAt(VisibleTexts(field, fieldPath, paragraph.GetParagraphPath()), copiedIndex), path);

        var relativePath = HtmlToJsonConverter.ToRelativePath(fieldPath, path);
        return (relativePath is null ? null : field.SelectToken(relativePath) as JValue, path);
    }
    
    private static int? CopiedTextIndex(HtmlNode span, string path)
    {
        var paragraph = span.OwnerDocument.DocumentNode.Descendants()
            .Where(x => x.HasKey())
            .Where(x => IsUnder(path, x.GetParagraphPath()))
            .MaxBy(x => x.GetParagraphPath().Length);

        var container = paragraph?.AncestorsAndSelf().FirstOrDefault(x => x.IsJsonRichText());
        if (container?.FieldValue() is not { } field)
            return null;

        var fieldPath = container.Attributes[ConversionConstants.PathAttr].Value;
        var index = VisibleTexts(field, fieldPath, paragraph!.GetParagraphPath()).FindIndex(x => $"{fieldPath}.{x.Path}" == path);

        return index < 0 ? null : index;
    }

    private static JValue? TextAt(List<JValue> texts, int index) => index >= 0 && index < texts.Count ? texts[index] : null;
    
    private static List<JValue> VisibleTexts(JToken field, string fieldPath, string paragraphPath)
    {
        var relativePath = HtmlToJsonConverter.ToRelativePath(fieldPath, paragraphPath);
        var paragraph = relativePath switch
        {
            null => null,
            "" => field,
            _ => field.SelectToken(relativePath)
        } as JContainer;

        return paragraph?.Descendants()
            .OfType<JProperty>()
            .Where(x => 
                x.Name == "text" && 
                x.Value is JValue { Type: JTokenType.String } && 
                !string.IsNullOrWhiteSpace(x.Value.Value<string>()))
            .Select(x => (JValue)x.Value)
            .ToList() ?? [];
    }

    private static bool IsUnder(string path, string paragraphPath) => path.StartsWith(paragraphPath + ".", StringComparison.Ordinal);
}