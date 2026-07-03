using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Models.Response.Entry;

public record DownloadEntryResponse(FileReference Content) : IDownloadContentOutput
{
    public FileReference Content { get; set; } = Content;

    [Display("Referenced entry UIDs")]
    public IEnumerable<string>? ReferencedEntryUids { get; set; }

    [Display("Referenced entries")]
    public IEnumerable<EntryReferenceItem>? ReferencedEntries { get; set; }
}
