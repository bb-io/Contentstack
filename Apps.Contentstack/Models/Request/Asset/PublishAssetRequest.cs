using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Asset;

public class PublishAssetRequest : EnvironmentRequest
{
    [DataSource(typeof(LanguageDataHandler))]
    public string Locale { get; set; } = string.Empty;

    [Display("Scheduled at", Description = "In case of scheduled publishing, add this optional input")]
    public DateTime? ScheduledAt { get; set; }
}
