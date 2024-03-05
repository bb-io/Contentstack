using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Workflow;
using Apps.Contentstack.Models.Response.Workflow;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class WorkflowStageDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    private string? WorkflowId { get; }

    public WorkflowStageDataHandler(InvocationContext invocationContext,
        [ActionParameter] WorkflowStageRequest filterRequest) : base(invocationContext)
    {
        WorkflowId = filterRequest.WorkflowId;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(WorkflowId))
            throw new("You should input Workflow ID first");

        var request = new ContentstackRequest($"v3/workflows/{WorkflowId}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<WorkflowResponse>(request);

        return response.Workflow.WorkflowStages
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Uid, x => x.Name);
    }
}