using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Entities;

public class WebhookEntity
{
    public string Uid { get; set; }

    public string Name { get; set; }

    public IEnumerable<string> Channels { get; set; }

    public IEnumerable<WebhookDestinationEntity> Destinations { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<string>? Branches { get; set; }
    
    public string RetryPolicy { get; set; }
}
