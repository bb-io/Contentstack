using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

public static class EntryPayloadValidator
{
    public static List<string> Validate(JObject before, JObject after)
    {
        var violations = new List<string>();

        CheckNoTransportMarkers(before, after, violations);
        CheckRichTextFieldsStayDocuments(before, after, violations);
        CheckBlockInstanceUidsSurvive(before, after, violations);

        return violations;
    }

    public static List<string> Inspect(JObject before, JObject after)
    {
        var warnings = new List<string>();

        CheckRichTextNodeUidsSurvive(before, after, warnings);

        return warnings;
    }

    private static void CheckNoTransportMarkers(JObject before, JObject after, ICollection<string> violations)
    {
        foreach (var value in Strings(after))
        {
            var text = value.Value<string>();

            if (!TransportMarker.IsPresentIn(text))
                continue;

            if (before.SelectToken(value.Path) is JValue existing && existing.Value<string>() == text)
                continue;

            violations.Add($"'{value.Path}' would be filled with the file's own field markers instead of content: "
                           + TransportMarker.Describe(text!));
        }
    }

    private static void CheckRichTextFieldsStayDocuments(JObject before, JObject after,
        ICollection<string> violations)
    {
        foreach (var (path, _) in RichTextDocuments(before))
        {
            var value = after.SelectToken(path);

            if (value is null)
                continue;

            if (value is JObject document && document["type"]?.Value<string>() == "doc" &&
                document["children"] is JArray)
                continue;

            violations.Add(
                $"'{path}' is a JSON rich text field but would be saved as {Describe(value)} instead of a document.");
        }
    }

    private static void CheckBlockInstanceUidsSurvive(JObject before, JObject after,
        ICollection<string> violations)
    {
        var kept = BlockUidsByList(after);

        var duplicates = kept.SelectMany(x => x.Value)
            .GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

        if (duplicates.Count > 0)
            violations.Add("the same block instance id would be saved more than once: " + Join(duplicates));

        foreach (var (list, had) in BlockUidsByList(before))
        {
            var now = kept.TryGetValue(list, out var uids) ? uids : [];

            if (now.Count > had.Count)
            {
                violations.Add($"'{list}' holds {had.Count} block instances but {now.Count} would be saved.");
                continue;
            }

            var moved = now.Where((uid, i) => uid != had[i]).ToList();

            if (moved.Count > 0)
                violations.Add($"block instances in '{list}' would no longer sit where the entry has them: "
                               + Join(moved));
        }
    }

    private static void CheckRichTextNodeUidsSurvive(JObject before, JObject after,
        ICollection<string> violations)
    {
        foreach (var (path, document) in RichTextDocuments(before))
        {
            if (after.SelectToken(path) is not JObject updated)
                continue;

            var kept = NodeUids(updated);

            var duplicates = kept.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicates.Count > 0)
                violations.Add($"rich text field '{path}' would repeat the node ids {Join(duplicates)}.");

            var lost = NodeUids(document).Except(kept).ToList();
            if (lost.Count > 0)
                violations.Add($"rich text field '{path}' would lose the node ids {Join(lost)}.");
        }
    }

    private static Dictionary<string, List<string>> BlockUidsByList(JObject entry)
    {
        var lists = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var block in entry.Descendants().OfType<JObject>())
        {
            var uid = block["_metadata"]?["uid"]?.Value<string>();

            if (string.IsNullOrEmpty(uid))
                continue;

            var bracket = block.Path.LastIndexOf('[');
            var list = bracket < 0 ? block.Path : block.Path[..bracket];

            if (!lists.TryGetValue(list, out var uids))
                lists[list] = uids = [];

            uids.Add(uid);
        }

        return lists;
    }

    private static IEnumerable<(string Path, JObject Document)> RichTextDocuments(JObject entry)
        => entry.DescendantsAndSelf()
            .OfType<JObject>()
            .Where(x => x["type"]?.Value<string>() == "doc" && x["children"] is JArray)
            .Select(x => (x.Path, x));

    private static List<string> NodeUids(JObject document)
        => document.DescendantsAndSelf()
            .OfType<JObject>()
            .Select(x => x["uid"]?.Value<string>())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList()!;

    private static IEnumerable<JValue> Strings(JObject entry)
        => entry.Descendants().OfType<JValue>().Where(x => x.Type == JTokenType.String);

    private static string Describe(JToken? value)
        => value.Type == JTokenType.String ? "text" : value.Type.ToString().ToLowerInvariant();

    private static string Join(IEnumerable<string> values) => string.Join(", ", values);
}
