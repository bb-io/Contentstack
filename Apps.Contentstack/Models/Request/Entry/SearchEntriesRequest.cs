using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class SearchEntriesRequest
{
    [Display("Content types")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public IEnumerable<string>? ContentTypeIds { get; set; }
}
