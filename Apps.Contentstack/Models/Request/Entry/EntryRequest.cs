using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.DataSourceHandlers.Entry;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Models.Request.Entry;

public class EntryRequest : IDownloadContentInput
{
    [Display("Content type ID")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string ContentTypeId { get; set; }
    
    [Display("Entry ID")]
    [DataSource(typeof(SimpleEntryDataHandler))]
    public string ContentId { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ContentTypeId))
            throw new PluginMisconfigurationException("'Content type ID' input is required.");
        if (string.IsNullOrEmpty(ContentId))
            throw new PluginMisconfigurationException("'Entry ID' input is required.");
    }
}