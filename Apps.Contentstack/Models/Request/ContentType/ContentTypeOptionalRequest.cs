using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.ContentType;

public class ContentTypeOptionalRequest
{
    [Display("Content type ID")]
    [DataSource(typeof(ContentTypeDataHandler))]
    public string? ContentTypeId { get; set; }

    [Display("Locale")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? Locale { get; set; }

    public string? Tag { get; set; }
}
