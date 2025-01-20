using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request;

public class EnvironmentRequest
{
    [DataSource(typeof(EnvironmentDataHandler))]
    public string Environment { get; set; } = string.Empty;
}