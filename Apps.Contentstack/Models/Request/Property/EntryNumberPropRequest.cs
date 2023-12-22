using Apps.Contentstack.DataSourceHandlers.Properties;
using Apps.Contentstack.Models.Request.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Property;

public class EntryNumberPropRequest : EntryRequest
{
    [DataSource(typeof(EntryNumberPropDataHandler))]
    public string Property { get; set; }
}