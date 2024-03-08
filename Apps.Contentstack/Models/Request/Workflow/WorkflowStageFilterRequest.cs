using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Workflow;

public class WorkflowStageFilterRequest
{
    [Display("Workflow ID")]
    [DataSource(typeof(WorkflowDataHandler))]
    public string? WorkId { get; set; }
    
    [Display("Workflow stage")]
    [DataSource(typeof(WorkflowStageFilterDataHandler))]
    public string? WorkflowStageId { get; set; }
}