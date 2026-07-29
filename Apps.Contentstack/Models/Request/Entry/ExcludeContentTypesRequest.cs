using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class ExcludeContentTypesRequest
{
    [Display("Exclude content type IDs", Description = "Optional list of content type IDs whose referenced entries are skipped")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public IEnumerable<string>? ExcludeContentTypeIds { get; set; }
}
