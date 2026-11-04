using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class PublishedByFilterRequest
{
    [Display("Not published by", Description = "Excludes entries published by any of these users in the selected environment and locale")]
    [DataSource(typeof(UserDataHandler))]
    public IEnumerable<string>? ExcludedPublisherUserIds { get; set; }
}
