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

    [Display("Created fields", Description = "Fields that did not exist in the target locale's entry and were created from the file. Contentstack keeps a localized entry in the shape it had when it was localized, so fields added to the content type afterwards are absent until an upload creates them.")]
    public IEnumerable<string>? CreatedFields { get; set; }
}