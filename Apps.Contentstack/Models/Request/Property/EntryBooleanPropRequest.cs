using Apps.Contentstack.DataSourceHandlers.Properties;
using Apps.Contentstack.Models.Request.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Property;

public class EntryBooleanPropRequest : EntryRequest
{
    [DataSource(typeof(EntryBooleanPropDataHandler))]
    public string Property { get; set; }
}