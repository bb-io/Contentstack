using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Asset;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class AssetDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public AssetDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/assets", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListAssetsResponse>(request);
        
        return response.Assets
            .Where(x => context.SearchString is null ||
                        x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToDictionary(x => x.Uid, x => x.Title);
    }
}