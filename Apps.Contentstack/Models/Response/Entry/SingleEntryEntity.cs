using Apps.Contentstack.Models.Entities;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Entry;

public class SingleEntryEntity : EntryEntity
{
    [Display("Asset IDs")]
    public List<string> AssetIds { get; set; } = new();

    public SingleEntryEntity(EntryEntity entry, List<string> assetIds)
    {
        Uid = entry.Uid;
        Locale = entry.Locale;
        Title = entry.Title;
        CreatedAt = entry.CreatedAt;
        Tags = entry.Tags;
        InProgress = entry.InProgress;
        Workflow = entry.Workflow;
        AssetIds = assetIds;
        Version = entry.Version;
    }
}