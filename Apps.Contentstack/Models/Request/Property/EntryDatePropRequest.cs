using Apps.Contentstack.DataSourceHandlers.Properties;
using Apps.Contentstack.Models.Request.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Property;

public class EntryDatePropRequest : EntryRequest
{
    [DataSource(typeof(EntryDatePropDataHandler))]
    public string Property { get; set; }
}