using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class EnvironmentDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/environments", Method.Get, Creds)
            .AddParameter("include_count", "false", ParameterType.QueryString)
            .AddParameter("asc", "created_at", ParameterType.QueryString)            
            .AddParameter("desc", "updated_at", ParameterType.QueryString);
        
        var response = await Client.ExecuteWithErrorHandling<ListEnvironmentsResponse>(request);
        
        return response.Environments
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Uid, x.Name));
    }
}