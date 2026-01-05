using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response;

public class GetEntryFileMetadataResponse
{
    [Display("Content type ID")]
    public string ContentTypeId { get; set; } = string.Empty;
    
    [Display("Entry ID")]
    public string EntryId { get; set; } = string.Empty;
}