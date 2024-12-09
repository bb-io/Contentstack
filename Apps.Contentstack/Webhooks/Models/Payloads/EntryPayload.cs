using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Webhooks.Models.Payloads;

public class EntryPayload
{
    [Display("Entry ID")]
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
}