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
        var credsList = creds.ToList();
        
        this.AddHeader("api_key", credsList.Get(CredsNames.StackApiKey).Value);
        this.AddHeader("authorization", credsList.Get(CredsNames.ManagementToken).Value);

        string? connectionType = credsList.FirstOrDefault(x => x.KeyName == CredsNames.ConnectionType)?.Value;
        if (connectionType == ConnectionType.BranchScopedApiKey)
            this.AddHeader("branch", credsList.Get(CredsNames.BranchName).Value);
    }
}