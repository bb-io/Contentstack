using Apps.Contentstack.DataSourceHandlers.Properties;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class EntryStringPropRequest : EntryRequest
{
    [DataSource(typeof(EntryStringPropDataHandler))]
    public string Property { get; set; }
}