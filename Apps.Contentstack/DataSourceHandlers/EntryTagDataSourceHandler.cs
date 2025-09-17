using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Response.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Contentstack.DataSourceHandlers;
public class EntryTagDataSourceHandler : AppInvocable, IAsyncDataSourceHandler
{
    private readonly EntryRequest entry;
    private readonly LocaleRequest locale;

    public EntryTagDataSourceHandler(InvocationContext ctx,[ActionParameter] EntryRequest entry,[ActionParameter] LocaleRequest locale) : base(ctx)
    {
        this.entry = entry;
        this.locale = locale;
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext _, CancellationToken __)
    {
        var endpoint = $"v3/content_types/{entry.ContentTypeId}/entries/{entry.EntryId}";
        if (!string.IsNullOrWhiteSpace(locale?.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var req = new ContentstackRequest(endpoint, Method.Get, Creds);
        var resp = await Client.ExecuteWithErrorHandling<EntryGenericResponse>(req);

        var tags = (resp.Entry?["tags"] as JArray)?
            .Select(t => t.ToString())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(t => t, t => t, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return tags;
    }
}
