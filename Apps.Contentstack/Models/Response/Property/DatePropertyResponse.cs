using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Property;

public class DatePropertyResponse
{
    [Display("Property ID")] public string Uid { get; set; }

    public DateTime Value { get; set; }
}