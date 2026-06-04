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

    [Display("Exclude locale", Description = "If set, the event will not fire for entries in this locale")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? ExcludeLocale { get; set; }

    public string? Tag { get; set; }

    [Display("Updated by user ID", Description = "Only fire this event if the entry was updated by this user")]
    public string? UpdatedByUserId { get; set; }

    [Display("Not updated by user ID", Description = "Skip this event if the entry was updated by this user")]
    public string? NotUpdatedByUserId { get; set; }
}
