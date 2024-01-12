using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Request.Webhook;

public class CreateWebhookRequest
{
    public WebhookEntity Webhook { get; set; }
}