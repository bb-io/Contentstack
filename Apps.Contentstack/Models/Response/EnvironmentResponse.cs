using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response;

public class EnvironmentResponse
{
    [Display("uid")]
    public string Uid { get; set; } = string.Empty;
    
    [Display("name")]
    public string Name { get; set; } = string.Empty;
}