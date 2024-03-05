using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Workflow;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class WorkflowDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public WorkflowDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/workflows", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListWorkflowsResponse>(request);
        
        return response.Workflows
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToDictionary(x => x.Uid, x => x.Name);
    }
}