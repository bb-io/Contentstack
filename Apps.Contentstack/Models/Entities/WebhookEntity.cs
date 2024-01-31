namespace Apps.Contentstack.Models.Entities;

public class WebhookEntity
{
    public string Uid { get; set; }

    public string Name { get; set; }

    public IEnumerable<string> Channels { get; set; }

    public IEnumerable<WebhookDestinationEntity> Destinations { get; set; }
    
    public string RetryPolicy { get; set; }
}