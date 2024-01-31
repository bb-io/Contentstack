using Apps.Contentstack.Webhooks.Models.Payloads;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Webhooks.Models;

public class EntryWebhookResponse
{
    public EntryPayload Entry { get; set; }
    
    [Display("Content type")]
    public ContentTypePayload ContentType { get; set; }
}