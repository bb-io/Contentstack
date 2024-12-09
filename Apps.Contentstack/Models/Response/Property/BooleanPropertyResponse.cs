using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Property;

public class BooleanPropertyResponse
{
    [Display("Property ID")] public string Uid { get; set; }

    public bool Value { get; set; }
}