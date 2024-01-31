using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.Webhook;

public class WebhooksResponse
{
    public IEnumerable<WebhookEntity> Webhooks { get; set; }
}