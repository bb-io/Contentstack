using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Helper;

public static class PublishDetailsFilter
{
    public static bool IsPublishedByAny(EntryEntity entry, IReadOnlyCollection<string> userIds, string? environmentUid,
        string? locale)
    {
        var publishLocale = string.IsNullOrWhiteSpace(locale) ? entry.Locale : locale;

        return entry.PublishDetails.Any(detail =>
            userIds.Contains(detail.User, StringComparer.OrdinalIgnoreCase)
            && detail.Locale.Equals(publishLocale, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(environmentUid)
                || detail.Environment.Equals(environmentUid, StringComparison.OrdinalIgnoreCase)));
    }
}
