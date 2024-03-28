using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Workflow;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class WorkflowStageDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public WorkflowStageDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/workflows/", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListWorkflowsResponse>(request);

        return response.Workflows
            .SelectMany(y => y.WorkflowStages.Select(x => (x.Uid, $"{y.Name} - {x.Name}")))
            .Where(x => context.SearchString is null ||
                        x.Item2.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Uid, x => x.Item2);
    }
}