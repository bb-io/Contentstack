using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Response.Entry;

public class CalculateAllEntriesResponse
{
    [Display("Entries count")]
    public int EntriesCount { get; set; }
}