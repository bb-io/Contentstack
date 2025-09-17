using Apps.Contentstack.Actions;
using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Response.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;
public class TagDataSourceHandler : AppInvocable, IAsyncDataSourceHandler
{
    public TagDataSourceHandler(InvocationContext invocationContext) : base(invocationContext) { }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken ct)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var contentTypes = await new ContentTypesActions(InvocationContext).ListContentTypes(null);
        const int pageSize = 100;

        foreach (var ctDef in contentTypes.Items ?? Enumerable.Empty<dynamic>())
        {
            int skip = 0;
            while (!ct.IsCancellationRequested)
            {
                var endpoint = $"v3/content_types/{ctDef.Uid}/entries"
                    .SetQueryParameter("include_workflow", "true")
                    .SetQueryParameter("skip", skip.ToString())
                    .SetQueryParameter("limit", pageSize.ToString());

                var req = new ContentstackRequest(endpoint, Method.Get, Creds);
                var resp = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(req);

                var page = (resp.Entries ?? Enumerable.Empty<EntryEntity>()).ToArray();

                foreach (var entry in page)
                {
                    if (entry.Tags?.Any() == true)
                    {
                        foreach (var t in entry.Tags)
                            tags.Add(t);
                    }
                }

                if (page.Length < pageSize) break;
                skip += pageSize;

                if (!string.IsNullOrWhiteSpace(context?.SearchString) && tags.Count > 200) break;
            }

            if (!string.IsNullOrWhiteSpace(context?.SearchString) && tags.Count > 200) break;
        }

        var filter = context?.SearchString?.Trim();
        var result = string.IsNullOrWhiteSpace(filter)
            ? tags.AsEnumerable()
            : tags.Where(t => t.Contains(filter!, StringComparison.OrdinalIgnoreCase));

        return result
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToDictionary(t => t, t => t, StringComparer.OrdinalIgnoreCase);
    }
}