using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Contentstack.Models.Request.Workflow;

public class WorkflowStageFilterRequest
{
    [Display("Workflow stage")]
    [DataSource(typeof(WorkflowStageDataHandler))]
    public string? WorkflowStageId { get; set; }
}