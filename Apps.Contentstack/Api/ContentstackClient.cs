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

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        if (response.Content == null)
        {
            return new(response.ErrorMessage);
        }

        var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content!, JsonSettings)!;
        return new PluginApplicationException(error.ErrorMessage + $"; {error.Errors}");
    }
}