using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Entry;

public class UpdateEntryFromHtmlResponse
{
    [Display("Content type ID")]
    public string ContentTypeId { get; set; } = string.Empty;
    
    [Display("Entry ID")]
    public string EntryId { get; set; } = string.Empty;
}