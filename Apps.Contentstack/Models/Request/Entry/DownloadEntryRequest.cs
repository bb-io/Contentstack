using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.DataSourceHandlers.Entry;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Models.Request.Entry;

public class DownloadEntryRequest : IDownloadContentInput
{
    [Display("Content type ID")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string ContentTypeId { get; set; } = string.Empty;

    [Display("Entry ID")]
    [DataSource(typeof(SimpleEntryDataHandler))]
    public string ContentId { get; set; } = string.Empty;

    [Display("Exclude field IDs", Description = "Optional list of field IDs to exclude from the generated HTML file")]
    public IEnumerable<string>? ExcludeFieldIds { get; set; }

    [Display("Include referenced entry UIDs", Description = "Optionally include referenced entry UIDs from the root entry in the action output")]
    public bool IncludeReferencedEntryUids { get; set; }

    [Display("Include referenced entry content", Description = "If enabled, the HTML file will include the translatable content of all referenced entries. These will be updated when the file is uploaded back via 'Upload entry content'.")]
    public bool IncludeReferencedEntryContent { get; set; }

    public void Validate()
    {
        if (string.IsNullOrEmpty(ContentTypeId))
            throw new PluginMisconfigurationException("'Content type ID' input is required.");
        if (string.IsNullOrEmpty(ContentId))
            throw new PluginMisconfigurationException("'Entry ID' input is required.");
    }
}
