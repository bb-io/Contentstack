using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Property;

public class StringPropertyResponse
{
    [Display("Property UID")] public string Uid { get; set; }

    public string Value { get; set; }
}