using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Contentstack.Api;

public class ContentstackClient(AuthenticationCredentialsProvider[] creds) : BlackBirdRestClient(new()
{
    BaseUrl = $"{creds.Get(CredsNames.Host).Value}".ToUri()
})
{
    protected override JsonSerializerSettings? JsonSettings => JsonConfig.Settings;

    public async Task<List<TItem>> Paginate<TResponse, TItem>(
        RestRequest request,
        Func<TResponse, IEnumerable<TItem>?> selector,
        Func<List<TItem>, bool>? stopWhen = null,
        int pageSize = 100)
    {
        var results = new List<TItem>();
        int skip = 0;

        while (true)
        {
            request.AddOrUpdateParameter(new QueryParameter("skip", skip.ToString()));
            request.AddOrUpdateParameter(new QueryParameter("limit", pageSize.ToString()));

            var response = await ExecuteWithErrorHandling<TResponse>(request);
            var page = (selector(response) ?? []).ToList();

            results.AddRange(page);

            if (page.Count < pageSize || (stopWhen?.Invoke(results) ?? false))
                break;

            skip += pageSize;
        }

        return results;
    }
    
    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;

        T? val;
        try
        {
            val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        }
        catch (JsonException)
        {
            // A successful status code does not guarantee a JSON body: proxies and gateways can answer with HTML.
            throw new PluginApplicationException(ContentstackErrorParser.BuildParseFailureMessage(typeof(T), content));
        }

        if (val == null)
        {
            throw new PluginApplicationException(ContentstackErrorParser.BuildParseFailureMessage(typeof(T), content));
        }

        return val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ContentstackRetryPolicies.ExecuteWithRateLimitRetry(() => ExecuteAsync(request));
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }


    protected override Exception ConfigureErrorException(RestResponse response)
        => ContentstackErrorParser.BuildException(response.StatusCode, response.Content, response.ErrorMessage);
}
