using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Response.Asset;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Contentstack.Helper;

public class AssetHelper(InvocationContext context) : AppInvocable(context)
{
    public Task UpdateEntryWithAssets(string contentTypeId, string entryId, JObject entryObject, string? locale)
    {
        var endpoint = $"v3/content_types/{contentTypeId}/entries/{entryId}?locale={locale}";
        var request = new ContentstackRequest(endpoint, Method.Put, Creds).WithJsonBody(new { entry = entryObject });

        return Client.ExecuteWithErrorHandling(request);
    }
    
    public async Task<Dictionary<string, AssetEntity>> FindAssetsByNames(ISet<string> names)
    {
        var result = new Dictionary<string, AssetEntity>(StringComparer.OrdinalIgnoreCase);
        if (names.Count == 0)
            return result;

        var request = new ContentstackRequest("v3/assets", Method.Get, Creds);

        var assets = await Client.Paginate<ListAssetsResponse, AssetEntity>(
            request, 
            r => r.Assets,
            collected => names.All(n => collected.Any(a => a.GetNames().Contains(n, StringComparer.OrdinalIgnoreCase))));

        foreach (var asset in assets)
        {
            foreach (var name in asset.GetNames().Where(names.Contains))
                result.TryAdd(name, asset);
        }

        return result;
    }
}