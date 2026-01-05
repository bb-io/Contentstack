using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.DataSourceHandlers.Entry;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Models.Request.Entry;

public class UploadEntryRequest : IUploadContentInput
{
    [Display("Entry ID")]
    [DataSource(typeof(SimpleEntryDataHandler))]
    public string? ContentId { get; set; }

    [Display("Content type ID")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string? ContentTypeId { get; set; }

    [Display("Content")]
    public FileReference Content { get; set; }

    [Display("Locale")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? Locale { get; set; }
}
