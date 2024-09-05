using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Request;

public class FileExtensionRequest
{
    [Display("File extension", Description = "The file extension to filter by. Example: '.jpg', '.png' or '.json'")]
    public string? FileExtension { get; set; }
}