using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties.Base;

public abstract class EntryPropDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    private string ContentTypeId { get; }

    protected string EntryId { get; }

    protected abstract string DataType { get; }

    protected EntryPropDataHandler(InvocationContext invocationContext, EntryRequest request) : base(invocationContext)
    {
        EntryId = request.EntryId;
        ContentTypeId = request.ContentTypeId;
    }


    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ContentTypeId))
            throw new("You have to input Content type first");

        if (string.IsNullOrWhiteSpace(EntryId))
            throw new("You have to input Entry first");

        var response = await Client.ContentType(ContentTypeId).FetchAsync();
        var contentType = Client.ProcessResponse<ContentTypeResponse>(response).ContentType;

        return contentType.Schema
            .Where(x => x.DataType == DataType)
            .Where(x => context.SearchString is null ||
                        x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Uid, x => x.DisplayName);
    }
}