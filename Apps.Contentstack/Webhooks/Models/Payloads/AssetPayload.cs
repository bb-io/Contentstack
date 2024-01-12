using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Webhooks.Models.Payloads;

public class AssetPayload
{
    [Display("UID")]
    public string Uid { get; set; }
    
    [Display("File name")]
    public string Filename { get; set; }
    
    public string Title { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
    
    public string Url { get; set; }
}