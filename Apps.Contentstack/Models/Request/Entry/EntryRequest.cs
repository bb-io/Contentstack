using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Entry;

public class EntryRequest
{
    [Display("Content type")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string ContentTypeId { get; set; }
    
    [Display("Entry")]
    [DataSource(typeof(EntryDataHandler))]
    public string EntryId { get; set; }
}