using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.HtmlConversion;

/// <summary>
/// The file only carries text, so a localized entry keeps whatever references, assets and block list it held
/// before. This brings those over from the source-locale entry ahead of the import: fields that hold data are
/// copied, fields that hold text keep the locale's value, and lists of blocks and groups follow the source's
/// order, matched by their instance id and otherwise by position.
/// </summary>
public static class SourceEntrySync
{
    private static readonly HashSet<string> DataTypes = new(StringComparer.Ordinal)
    {
        "reference", "file", "number", "boolean", "isodate", "taxonomy"
    };

    public static void Apply(JObject target, JObject source, JArray schema, ISet<string>? excludedFieldIds = null)
    {
        excludedFieldIds ??= new HashSet<string>();

        foreach (var field in schema.OfType<JObject>())
        {
            var uid = field["uid"]?.ToString();

            if (string.IsNullOrEmpty(uid) || excludedFieldIds.Contains(uid))
                continue;

            if (Flag(field, "non_localizable"))
            {
                Mirror(target, source, uid);
                continue;
            }

            switch (field["data_type"]?.ToString())
            {
                case { } type when DataTypes.Contains(type):
                    Mirror(target, source, uid);
                    break;

                case "blocks":
                    SyncList(target, source, uid, item => BlockSchema(field, item), unwrap: true, excludedFieldIds);
                    break;

                case "group" or "global_field" when Flag(field, "multiple"):
                    SyncList(target, source, uid, _ => field["schema"] as JArray, unwrap: false, excludedFieldIds);
                    break;

                case "group" or "global_field":
                    SyncObject(target, source, uid, field["schema"] as JArray, excludedFieldIds);
                    break;

                default:
                    FillMissing(target, source, uid);
                    break;
            }
        }
    }

    private static void Mirror(JObject target, JObject source, string uid)
    {
        if (source.TryGetValue(uid, out var value))
            target[uid] = value.DeepClone();
        else
            target.Remove(uid);
    }

    private static void FillMissing(JObject target, JObject source, string uid)
    {
        if (target[uid] is { Type: not JTokenType.Null } || !source.TryGetValue(uid, out var value))
            return;

        target[uid] = value.DeepClone();
    }

    private static void SyncObject(JObject target, JObject source, string uid, JArray? schema,
        ISet<string> excludedFieldIds)
    {
        if (schema is not null && source[uid] is JObject from && target[uid] is JObject into)
        {
            Apply(into, from, schema, excludedFieldIds);
            return;
        }

        Mirror(target, source, uid);
    }

    private static void SyncList(JObject target, JObject source, string uid, Func<JObject, JArray?> itemSchema,
        bool unwrap, ISet<string> excludedFieldIds)
    {
        if (source[uid] is not JArray from || target[uid] is not JArray into)
        {
            Mirror(target, source, uid);
            return;
        }

        var sourceUids = from.Select(InstanceUid).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
        var used = new HashSet<int>();
        var synced = new JArray();

        for (int i = 0; i < from.Count; i++)
        {
            var sourceItem = from[i];
            var match = sourceItem is JObject ? FindMatch(into, sourceItem, i, sourceUids, used, unwrap) : -1;

            if (match < 0)
            {
                synced.Add(sourceItem.DeepClone());
                continue;
            }

            used.Add(match);
            var item = (JObject)into[match].DeepClone();
            AdoptInstanceUid(item, (JObject)sourceItem, unwrap);
            var schema = itemSchema((JObject)sourceItem);

            if (schema is not null)
            {
                if (unwrap)
                    Apply((JObject)Inner(item)!, (JObject)Inner((JObject)sourceItem)!, schema, excludedFieldIds);
                else
                    Apply(item, (JObject)sourceItem, schema, excludedFieldIds);
            }

            synced.Add(item);
        }

        target[uid] = synced;
    }

    // An item in the same position stands in only when it is not claimed by id elsewhere and, for blocks,
    // is the same kind of block.
    private static int FindMatch(JArray into, JToken sourceItem, int index, ISet<string?> sourceUids,
        ISet<int> used, bool unwrap)
    {
        var uid = InstanceUid(sourceItem);

        if (uid is not null)
        {
            for (int j = 0; j < into.Count; j++)
                if (!used.Contains(j) && InstanceUid(into[j]) == uid)
                    return !unwrap || BlockType((JObject)into[j]) == BlockType((JObject)sourceItem) ? j : -1;
        }

        if (index >= into.Count || used.Contains(index) || into[index] is not JObject candidate)
            return -1;

        var candidateUid = InstanceUid(candidate);
        if (candidateUid is not null && sourceUids.Contains(candidateUid))
            return -1;

        if (unwrap && BlockType(candidate) != BlockType((JObject)sourceItem))
            return -1;

        return index;
    }

    private static bool Flag(JObject field, string name)
        => field[name] is JValue { Type: JTokenType.Boolean } value && value.Value<bool>();

    private static JArray? BlockSchema(JObject field, JObject item)
    {
        var type = BlockType(item);

        return field["blocks"]?
            .OfType<JObject>()
            .FirstOrDefault(x => x["uid"]?.ToString() == type)?["schema"] as JArray;
    }

    private static string? BlockType(JObject item)
        => item.Properties().FirstOrDefault(x => !x.Name.StartsWith('_') && x.Value is JObject)?.Name;

    private static JToken? Inner(JObject item)
    {
        var type = BlockType(item);
        return type is null ? null : item[type];
    }

    private static void AdoptInstanceUid(JObject item, JObject sourceItem, bool unwrap)
    {
        var uid = InstanceUid(sourceItem);
        if (uid is null)
            return;

        var holder = unwrap && Inner(item) is JObject inner && item["_metadata"] is null ? inner : item;

        if (holder["_metadata"] is JObject metadata)
            metadata["uid"] = uid;
        else
            holder["_metadata"] = new JObject { ["uid"] = uid };
    }

    // Block items keep their id either on the wrapper or on the block itself.
    private static string? InstanceUid(JToken item)
    {
        if (item is not JObject obj)
            return null;

        var uid = obj["_metadata"]?["uid"] ?? (Inner(obj) as JObject)?["_metadata"]?["uid"];
        return uid?.Type == JTokenType.String ? uid.ToString() : null;
    }
}
