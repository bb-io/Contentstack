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
    public async Task UpdateEntryWithAssets(string contentTypeId, string entryId, JObject entryObject)
    {
        var endpoint = $"v3/content_types/{contentTypeId}/entries/{entryId}";
        var request = new ContentstackRequest(endpoint, Method.Put, Creds).WithJsonBody(new { entry = entryObject });

        await Client.ExecuteWithErrorHandling(request);
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
            collected => names.All(n => collected.Any(a => string.Equals(a.Filename, n, StringComparison.OrdinalIgnoreCase))));

        foreach (var asset in assets)
        {
            if (!string.IsNullOrEmpty(asset.Filename) && names.Contains(asset.Filename))
                result.TryAdd(asset.Filename, asset);
        }

        return result;
    }
}