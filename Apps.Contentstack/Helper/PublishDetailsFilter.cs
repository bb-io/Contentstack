using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Helper;

public static class PublishDetailsFilter
{
    public static bool IsPublishedBy(EntryEntity entry, string userId, string? environmentUid, string? locale)
    {
        var publishLocale = string.IsNullOrWhiteSpace(locale) ? entry.Locale : locale;

        return entry.PublishDetails.Any(detail =>
            detail.User.Equals(userId, StringComparison.OrdinalIgnoreCase)
            && detail.Locale.Equals(publishLocale, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(environmentUid)
                || detail.Environment.Equals(environmentUid, StringComparison.OrdinalIgnoreCase)));
    }
}
