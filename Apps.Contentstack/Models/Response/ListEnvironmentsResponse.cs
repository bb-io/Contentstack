using Newtonsoft.Json;

namespace Apps.Contentstack.Models.Response;

public class ListEnvironmentsResponse
{
    [JsonProperty("environments")]
    public List<EnvironmentResponse> Environments { get; set; } = new();
}