using Apps.Contentstack.Constants;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        var allSchemas = contentType.Schema.Descendants()
            .Where(x => x is JObject && x["schema"] is not null)
            .SelectMany(x => x["schema"]!)
            .Concat(contentType.Schema)
            .Select(x => x.ToObject<SchemaResponse>(JsonSerializer.Create(JsonConfig.Settings))!)
            .ToArray();

        return allSchemas
            .Where(x => x.DataType == DataType)
            .Where(x => context.SearchString is null ||
                        x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(x => x.Uid)
            .ToDictionary(x => x.Uid, x => x.DisplayName);
    }
}