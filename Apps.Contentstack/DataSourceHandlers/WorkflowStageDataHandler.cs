using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Workflow;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class WorkflowStageDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/workflows/", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListWorkflowsResponse>(request);
        
        var dictionary = new Dictionary<string, string>();
        foreach (var workflow in response.Workflows)
        {
            foreach (var stage in workflow.WorkflowStages)
            {
                if (context.SearchString is not null && !stage.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                if(!dictionary.ContainsKey(stage.Uid))
                {               
                    dictionary.Add(stage.Uid, $"{workflow.Name} - {stage.Name}");
                }
            }
        }
        
        return dictionary.Select(x => new DataSourceItem(x.Key, x.Value));
    }
}