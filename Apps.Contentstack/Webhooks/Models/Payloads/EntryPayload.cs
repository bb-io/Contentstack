using Blackbird.Applications.Sdk.Common;
using Newtonsoft.Json;

namespace Apps.Contentstack.Webhooks.Models.Payloads;

public class EntryPayload
{
    [Display("Entry ID")]
    public string Uid { get; set; }

    [Display("Locale")]
    public string Locale { get; set; }

    public string Title { get; set; }

    public IEnumerable<string> Tags { get; set; }

    [Display("Created at")]
    public DateTime CreatedAt { get; set; }

    [Display("Updated by")]
    [JsonProperty("updated_by")]
    public string? UpdatedBy { get; set; }
}
