using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Contentstack.Models.Response.Entry;

public class UploadEntryResponse
{
    [Display("Content type ID")]
    public string ContentTypeId { get; set; } = string.Empty;
    
    [Display("Entry ID")]
    public string EntryId { get; set; } = string.Empty;

    public FileReference Content { get; set; }

    [Display("Errors", Description = "Values from the file that could not be imported and referenced entries that could not be updated. The main entry is always updated regardless of these errors.")]
    public IEnumerable<string>? Errors { get; set; }
}