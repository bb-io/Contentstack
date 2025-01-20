using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using RestSharp;

namespace Apps.Contentstack.Api;

public class ContentstackRequest(string resource, Method method, IEnumerable<AuthenticationCredentialsProvider> creds)
    : BlackBirdRestRequest(resource, method, creds)
{
    protected override void AddAuth(IEnumerable<AuthenticationCredentialsProvider> creds)
    {
        this.AddHeader("api_key", creds.Get(CredsNames.StackApiKey).Value);
        this.AddHeader("authorization", creds.Get(CredsNames.ManagementToken).Value);
    }
}