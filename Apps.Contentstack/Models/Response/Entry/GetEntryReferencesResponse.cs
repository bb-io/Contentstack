using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Response.Entry;

public class EntryReferenceItem
{
    [Display("Entry ID")]
    [JsonProperty("entry_uid")]
    public string EntryId { get; set; } = string.Empty;

    [Display("Content type ID")]
    [JsonProperty("content_type_uid")]
    public string ContentTypeId { get; set; } = string.Empty;
}

public class GetEntryReferencesResponse
{
    [Display("References")]
    [JsonProperty("references")]
    public IEnumerable<EntryReferenceItem> References { get; set; } = [];
}
