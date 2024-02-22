using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Entities;

public class ContentTypeEntity
{
    [Display("Content type UID")]
    public string Uid { get; set; }
    
    public string Title { get; set; }
    
    [Display("Created at")]
    public DateTime CreatedAt { get; set; }
}