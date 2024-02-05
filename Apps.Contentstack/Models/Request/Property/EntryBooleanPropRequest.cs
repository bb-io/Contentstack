using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.DataSourceHandlers.Entry;
using Apps.Contentstack.DataSourceHandlers.Properties;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Property;

public class EntryBooleanPropRequest
{
    [Display("Content type")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string ContentTypeId { get; set; }
    
    [Display("Entry")]
    [DataSource(typeof(EntryBooleanDataHandler))]
    public string EntryId { get; set; }
    
    [DataSource(typeof(EntryBooleanPropDataHandler))]
    public string Property { get; set; }
}