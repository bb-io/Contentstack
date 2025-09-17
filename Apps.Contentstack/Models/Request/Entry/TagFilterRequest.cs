using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry
{
    public class TagFilterRequest
    {
        [DataSource(typeof(TagDataSourceHandler))]
        public string? Tag { get; set; }
    }
}
