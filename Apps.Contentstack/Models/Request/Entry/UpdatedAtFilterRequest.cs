using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Request.Entry;

public class UpdatedAtFilterRequest
{
    [Display("Updated after")]
    public DateTime? UpdatedAfter { get; set; }

    [Display("Updated before")]
    public DateTime? UpdatedBefore { get; set; }
}
