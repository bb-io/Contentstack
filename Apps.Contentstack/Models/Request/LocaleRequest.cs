using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request;

public class LocaleRequest
{
    [DataSource(typeof(LanguageDataHandler))]
    public string? Locale { get; set; }
}