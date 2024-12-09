using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Property;

public class NumberPropertyResponse
{
    [Display("Property ID")] public string Uid { get; set; }

    public decimal Value { get; set; }
}