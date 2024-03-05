using Apps.Contentstack.Models.Entities;

namespace Apps.Contentstack.Models.Response.Workflow;

public class ListWorkflowsResponse
{
    public IEnumerable<WorkflowEntity> Workflows { get; set; }
}