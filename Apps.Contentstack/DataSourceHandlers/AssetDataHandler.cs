using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Asset;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers;

public class AssetDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public AssetDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var response = await Client.Asset().Query().FindAsync();
        var assets = Client.ProcessResponse<ListAssetsResponse>(response);

        return assets.Assets
            .Where(x => context.SearchString is null ||
                        x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToDictionary(x => x.Uid, x => x.Title);
    }
}