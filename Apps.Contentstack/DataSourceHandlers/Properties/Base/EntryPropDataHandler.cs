using Apps.Contentstack.Api;
using Apps.Contentstack.Constants;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers.Properties.Base;

public abstract class EntryPropDataHandler(InvocationContext invocationContext, string entryId, string contentTypeId)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    private string ContentTypeId { get; } = contentTypeId;

    protected string EntryId { get; } = entryId;

    protected abstract string DataType { get; }


    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ContentTypeId))
            throw new("You have to input Content type first");

        if (string.IsNullOrWhiteSpace(EntryId))
            throw new("You have to input Entry first");

        var request = new ContentstackRequest($"v3/content_types/{ContentTypeId}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ContentTypeResponse>(request);

        var allSchemas = response.ContentType.Schema.Descendants()
            .Where(x => x is JObject && x["schema"] is not null)
            .SelectMany(x => x["schema"]!)
            .Concat(response.ContentType.Schema)
            .Select(x => x.ToObject<SchemaResponse>(JsonSerializer.Create(JsonConfig.Settings))!)
            .ToArray();

        return allSchemas
            .Where(x => x.DataType == DataType)
            .Where(x => context.SearchString is null ||
                        x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Uid)
            .Select(x => new DataSourceItem(x.Uid, x.DisplayName));
    }
}