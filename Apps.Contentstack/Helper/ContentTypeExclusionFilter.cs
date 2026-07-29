using Apps.Contentstack.Models.Response.Entry;

namespace Apps.Contentstack.Helper;

public static class ContentTypeExclusionFilter
{
    public static bool IsExcluded(string? contentTypeId, IEnumerable<string>? excludedContentTypeIds)
    {
        if (string.IsNullOrWhiteSpace(contentTypeId))
            return false;

        var excluded = Normalize(excludedContentTypeIds);
        return excluded.Contains(contentTypeId);
    }

    public static IEnumerable<EntryReferenceItem> FilterReferences(
        IEnumerable<EntryReferenceItem>? references,
        IEnumerable<string>? excludedContentTypeIds)
    {
        if (references is null)
            return [];

        var excluded = Normalize(excludedContentTypeIds);
        if (excluded.Count == 0)
            return references;

        return references.Where(x => !excluded.Contains(x.ContentTypeId ?? string.Empty));
    }

    public static HashSet<(string EntryUid, string ContentTypeUid)> FilterReferencedEntries(
        IEnumerable<(string EntryUid, string ContentTypeUid)> referencedEntries,
        IEnumerable<string>? excludedContentTypeIds)
    {
        var excluded = Normalize(excludedContentTypeIds);

        return referencedEntries
            .Where(x => excluded.Count == 0 || !excluded.Contains(x.ContentTypeUid))
            .ToHashSet();
    }

    /// <summary>
    /// Drops the UIDs of entries that are only referenced through excluded content types.
    /// UIDs whose content type could not be resolved are kept, since we cannot prove they are excluded.
    /// </summary>
    public static List<string> FilterReferencedEntryUids(
        IEnumerable<string> referencedEntryUids,
        IEnumerable<(string EntryUid, string ContentTypeUid)> referencedEntries,
        IEnumerable<string>? excludedContentTypeIds)
    {
        var excluded = Normalize(excludedContentTypeIds);
        if (excluded.Count == 0)
            return referencedEntryUids.ToList();

        var allowedUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (entryUid, contentTypeUid) in referencedEntries)
        {
            if (excluded.Contains(contentTypeUid))
                excludedUids.Add(entryUid);
            else
                allowedUids.Add(entryUid);
        }

        return referencedEntryUids
            .Where(uid => allowedUids.Contains(uid) || !excludedUids.Contains(uid))
            .ToList();
    }

    private static HashSet<string> Normalize(IEnumerable<string>? contentTypeIds)
        => contentTypeIds is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : contentTypeIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
