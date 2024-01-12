using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Webhooks.Models.Payloads;

public class ContentTypePayload
{
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    public string Description { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
}