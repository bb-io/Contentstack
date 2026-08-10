using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class UpdatedByFilterRequest
{
    [Display("Not updated by (user IDs)")]
    public IEnumerable<string>? ExcludedUserIds { get; set; }
}
