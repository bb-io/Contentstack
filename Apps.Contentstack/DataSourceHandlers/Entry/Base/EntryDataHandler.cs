using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers.Entry.Base;

public class EntryDataHandler : AppInvocable, IAsyncDataSourceItemHandler
{
    private string ContentTypeId { get; }

    public EntryDataHandler(InvocationContext invocationContext, string contentTypeId) : base(
        invocationContext)
    {
        ContentTypeId = contentTypeId;
    }

    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ContentTypeId))
            throw new("You have to input Content type first");

        var request = new ContentstackRequest($"v3/content_types/{ContentTypeId}/entries", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(request);

        return response.Entries
            .Where(x => context.SearchString is null ||
                        x.Title.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new DataSourceItem(x.Uid, x.Title) );
    }
}