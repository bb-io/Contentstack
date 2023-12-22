using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Contentstack.Management.Core;
using Contentstack.Management.Core.Models;
using Newtonsoft.Json;

namespace Apps.Contentstack.Api;

public class AppClient : Stack
{
    public AppClient(AuthenticationCredentialsProvider[] creds) : base(new(new ContentstackClientOptions()
    {
        Host = creds.Get(CredsNames.Host).Value
    }), creds.Get(CredsNames.StackApiKey).Value, creds.Get(CredsNames.ManagementToken).Value)
    {
    }

    public T ProcessResponse<T>(ContentstackResponse response)
        => JsonConvert.DeserializeObject<T>(response.OpenResponse(), JsonConfig.Settings)!;
}