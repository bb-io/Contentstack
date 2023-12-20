using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers;

public class ContentTypeDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public ContentTypeDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var response = await Client.ContentType().Query().FindAsync();
        var items = Client.ProcessResponse<ListContentTypesResponse>(response).ContentTypes;

        return items
            .Where(x => context.SearchString is null ||
                        x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToDictionary(x => x.Uid, x => x.Title);
    }
}