using Blackbird.Applications.Sdk.Common.Exceptions;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Helper;

internal static class EntryPropertyResolver
{
    internal static T GetValue<T>(JObject entry, string propertyUid, string entryId, string? locale)
    {
        var value = ResolveExistingValue(entry, propertyUid, entryId, locale, allowMissing: false)!;

        if (!IsCompatible<T>(value))
        {
            throw new PluginMisconfigurationException(
                $"Property '{propertyUid}' in entry '{entryId}' for {DescribeLocale(locale)} " +
                $"contains a {DescribeTokenType(value.Type)} value, but the action expects {DescribeExpectedType<T>()}.");
        }

        try
        {
            var converted = value.ToObject<T>();
            if (converted is null)
                throw new InvalidOperationException("The property value is null.");

            return converted;
        }
        catch (Exception ex) when (ex is not PluginMisconfigurationException)
        {
            throw new PluginMisconfigurationException(
                $"Property '{propertyUid}' in entry '{entryId}' for {DescribeLocale(locale)} " +
                $"could not be read as {DescribeExpectedType<T>()}.");
        }
    }

    internal static bool TrySetExistingValue<T>(JObject entry, string propertyUid, T value, string entryId,
        string? locale)
    {
        var existing = ResolveExistingValue(entry, propertyUid, entryId, locale, allowMissing: true);
        if (existing is null)
            return false;

        existing.Value = value;
        return true;
    }

    internal static void CreateTopLevelValue<T>(JObject entry, JArray schema, string propertyUid, T value,
        string entryId, string? locale)
    {
        var schemaMatches = schema
            .OfType<JObject>()
            .Where(x => string.Equals(x["uid"]?.ToString(), propertyUid, StringComparison.Ordinal))
            .ToList();

        var expectedDataType = GetContentstackDataType<T>();
        var field = schemaMatches.Count == 1 ? schemaMatches[0] : null;
        var fieldDataType = field?["data_type"]?.ToString();
        var isMultiple = field?["multiple"]?.Value<bool>() == true;

        if (field is null || isMultiple || !string.Equals(fieldDataType, expectedDataType, StringComparison.Ordinal))
        {
            throw MissingPropertyException(propertyUid, entryId, locale);
        }

        entry[propertyUid] = value is null ? JValue.CreateNull() : JToken.FromObject(value);
    }

    private static JValue? ResolveExistingValue(JObject entry, string propertyUid, string entryId, string? locale,
        bool allowMissing)
    {
        var matches = entry.Descendants()
            .Where(x => x.Parent is JProperty property &&
                        string.Equals(property.Name, propertyUid, StringComparison.Ordinal))
            .Select(x => (JProperty)x.Parent!)
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            if (allowMissing)
                return null;

            throw MissingPropertyException(propertyUid, entryId, locale);
        }

        if (matches.Count > 1)
        {
            var paths = matches
                .Select(x => x.Path)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            throw new PluginMisconfigurationException(
                $"Property UID '{propertyUid}' is ambiguous in entry '{entryId}' for {DescribeLocale(locale)}. " +
                $"It matches {matches.Count} values ({string.Join(", ", paths)}). " +
                "This action cannot safely choose one of them.");
        }

        if (matches[0].Value is not JValue scalarValue)
        {
            throw new PluginMisconfigurationException(
                $"Property '{propertyUid}' in entry '{entryId}' for {DescribeLocale(locale)} has token type " +
                $"'{matches[0].Value.Type}'. This action supports only scalar properties.");
        }

        return scalarValue;
    }

    private static PluginMisconfigurationException MissingPropertyException(string propertyUid, string entryId,
        string? locale)
        => new(
            $"Property '{propertyUid}' was not found in entry '{entryId}' for {DescribeLocale(locale)}. " +
            "The field may be unset in this entry or locale, or it may belong to a group or modular block " +
            "that is not present. Only a unique top-level field can be created automatically.");

    private static bool IsCompatible<T>(JValue value)
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (value.Type is JTokenType.Null or JTokenType.Undefined)
            return false;
        if (type == typeof(string))
            return value.Type == JTokenType.String;
        if (type == typeof(bool))
            return value.Type == JTokenType.Boolean;
        if (type == typeof(decimal))
            return value.Type is JTokenType.Integer or JTokenType.Float;
        if (type == typeof(DateTime))
            return value.Type is JTokenType.Date or JTokenType.String;

        return true;
    }

    private static string GetContentstackDataType<T>()
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (type == typeof(string)) return "text";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(decimal)) return "number";
        if (type == typeof(DateTime)) return "isodate";

        throw new InvalidOperationException($"Unsupported entry property type '{type.Name}'.");
    }

    private static string DescribeExpectedType<T>()
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (type == typeof(string)) return "a string";
        if (type == typeof(bool)) return "a boolean";
        if (type == typeof(decimal)) return "a number";
        if (type == typeof(DateTime)) return "a date";
        return type.Name;
    }

    private static string DescribeTokenType(JTokenType type)
        => type.ToString().ToLowerInvariant();

    private static string DescribeLocale(string? locale)
        => string.IsNullOrWhiteSpace(locale) ? "the default locale" : $"locale '{locale}'";
}
