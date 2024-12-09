using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class ContentTypeDataHandler : AppInvocable, IAsyncDataSourceItemHandler
{
    public ContentTypeDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/content_types", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListContentTypesResponse>(request);
        
        return response.Items
            .Where(x => context.SearchString is null ||
                        x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new DataSourceItem(x.Uid, x.Title));
    }
}