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

    [Display("Sync non-translatable fields from source", Description = "If enabled, references, assets, numbers, booleans, dates, taxonomies and the list of modular blocks are copied from the source locale entry before the translations are applied, so the locale matches the source. Any of these fields the locale has changed on its own is overwritten.")]
    public bool SyncNonTranslatableFields { get; set; }

    [Display("Source locale", Description = "Locale to copy non-translatable fields from when 'Sync non-translatable fields from source' is enabled. Defaults to the master locale.")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? SourceLocale { get; set; }
}
