using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using RestSharp;

namespace Apps.Contentstack.Api;

public class ContentstackClient : BlackBirdRestClient
{
    protected override JsonSerializerSettings? JsonSettings => JsonConfig.Settings;

    public ContentstackClient(AuthenticationCredentialsProvider[] creds) : base(new()
    {
        BaseUrl = $"https://{creds.Get(CredsNames.Host).Value}".ToUri()
    })
    {
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content!, JsonSettings)!;
        return new(error.ErrorMessage + $"; {error.Errors}");
    }
}