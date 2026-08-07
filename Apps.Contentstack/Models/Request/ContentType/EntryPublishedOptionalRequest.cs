using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.ContentType;

public class EntryPublishedOptionalRequest : ContentTypeOptionalRequest
{
    [Display("Environment", Description = "Only fire this event if the entry was published to this environment")]
    [DataSource(typeof(EnvironmentDataHandler))]
    public string? Environment { get; set; }
}
