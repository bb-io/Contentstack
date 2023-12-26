using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Contentstack.Models.Request.Asset;

public class UploadAssetRequest
{
    public FileReference File { get; set; }
    
    [Display("Parent folder ID")]
    public string? ParentFolderId { get; set; }
    
    public string? Title { get; set; }
    
    public string? Description { get; set; }
}