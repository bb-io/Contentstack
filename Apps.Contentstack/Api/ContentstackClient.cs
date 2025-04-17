using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Response;
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

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        string content = (await ExecuteWithErrorHandling(request)).Content;
        T val = JsonConvert.DeserializeObject<T>(content, JsonSettings);
        if (val == null)
        {
            throw new Exception($"Could not parse {content} to {typeof(T)}");
        }

        return val;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        RestResponse restResponse = await ExecuteAsync(request);
        if (!restResponse.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(restResponse);
        }

        return restResponse;
    }


    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (string.IsNullOrEmpty(response.Content))
            return new PluginApplicationException(response.ErrorMessage);

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content, JsonSettings)!;

        if (error.Errors != null && error.Errors.TryGetValue("title", out var titleErrors))
        {
            var detail = string.Join("; ", titleErrors);
            return new PluginMisconfigurationException($"Field Title incorrect: {detail}");
        }

        return new PluginApplicationException(error.ErrorMessage + $"; {error.Errors}");
    }
}