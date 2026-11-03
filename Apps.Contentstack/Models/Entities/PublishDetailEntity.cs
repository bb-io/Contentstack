using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Entities;

public class PublishDetailEntity
{
    [JsonProperty("environment")]
    public string Environment { get; set; } = string.Empty;

    [JsonProperty("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonProperty("user")]
    public string User { get; set; } = string.Empty;
}
