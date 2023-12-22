using Apps.Contentstack.Api;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Contentstack.Management.Core.Exceptions;

namespace Apps.Contentstack.Connections;

public class ConnectionValidator : IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProviders,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = new AppClient(authenticationCredentialsProviders.ToArray());
            await client.Asset().Query().FindAsync();

            return new()
            {
                IsValid = true
            };
        }
        catch (ContentstackErrorException ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex.ErrorMessage
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