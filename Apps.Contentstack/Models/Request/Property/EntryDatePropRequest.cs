using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.DataSourceHandlers.Entry;
using Apps.Contentstack.DataSourceHandlers.Properties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Property;

public class EntryDatePropRequest
{
    [Display("Content type")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string ContentTypeId { get; set; }
    
    [Display("Entry")]
    [DataSource(typeof(EntryDateDataHandler))]
    public string EntryId { get; set; }
    
    [DataSource(typeof(EntryDatePropDataHandler))]
    public string Property { get; set; }
}