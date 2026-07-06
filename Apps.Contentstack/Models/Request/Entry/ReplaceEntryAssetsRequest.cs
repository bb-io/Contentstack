using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Request.Entry;

public class ReplaceEntryAssetsRequest
{
    [Display("Replace assets containing")]
    public string ReplaceAssetsContaining { get; set; } = string.Empty;

    [Display("With assets containing")]
    public string WithAssetsContaining { get; set; } = string.Empty;
}