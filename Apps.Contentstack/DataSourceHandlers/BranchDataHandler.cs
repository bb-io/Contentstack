using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class BranchDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/stacks/branches", Method.Get, Creds)
            .AddParameter("limit", "100", ParameterType.QueryString);

        var response = await Client.ExecuteWithErrorHandling<ListBranchesResponse>(request);

        return response.Branches
            .Where(x => context.SearchString is null ||
                        x.Uid.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Select(x => new DataSourceItem(x.Uid, x.Uid));
    }
}
