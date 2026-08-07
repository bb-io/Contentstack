namespace Apps.Contentstack.Webhooks.Models.Payloads;

public class EntryWebhookPayload
{
    public EntryPayload Entry { get; set; }    
    public ContentTypePayload ContentType { get; set; }
    public EnvironmentPayload? Environment { get; set; }
}
