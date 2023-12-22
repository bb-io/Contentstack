using Blackbird.Applications.Sdk.Common;
using File = Blackbird.Applications.Sdk.Common.Files.File;

namespace Apps.Contentstack.Models.Request.Asset;

public class UploadAssetRequest
{
    public File File { get; set; }
    
    [Display("Parent folder ID")]
    public string? ParentFolderId { get; set; }
    
    public string? Title { get; set; }
    
    public string? Description { get; set; }
}