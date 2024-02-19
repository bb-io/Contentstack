using Blackbird.Applications.Sdk.Common;

namespace Apps.Contentstack.Models.Request.Entry;

public class SearchEntriesRequest
{
    [Display("Workflow stage")]
    public string? WorkflowStage { get; set; }
}