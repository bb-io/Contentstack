using Apps.Contentstack.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using RestSharp;

namespace Apps.Contentstack.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        var creds = authenticationCredentialsProviders.ToArray();
        var client = new ContentstackClient(creds);
        var request = new ContentstackRequest("v3/assets", Method.Get, creds);
        
        try
        {
            var response = await client.ExecuteWithErrorHandling(request);
            return new()
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }
}