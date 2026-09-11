using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Contentstack.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    private readonly IEnumerable<ConnectionPropertyValue> _baseUrls =
    [
        new("https://api.contentstack.io", "US (North America, or NA)"),
        new("https://eu-api.contentstack.com", "Europe (EU)"),
        new("https://azure-na-api.contentstack.com", "Azure North America (Azure NA)"),
        new("https://azure-eu-api.contentstack.com", "Azure Europe (Azure EU)")
    ];
    
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = ConnectionType.ApiKey,
            DisplayName = "Developer API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.Host) 
                { 
                    DisplayName = "Host region",
                    DataItems = _baseUrls
                },
                new(CredsNames.StackApiKey) { DisplayName = "Stack API key", Sensitive = true },
                new(CredsNames.ManagementToken) { DisplayName = "Management token", Sensitive = true }
            }
        },
        new()
        {
            Name = ConnectionType.BranchScopedApiKey,
            DisplayName = "Developer API key (branch-scoped)",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.Host) 
                { 
                    DisplayName = "Host region",
                    DataItems = _baseUrls
                },
                new(CredsNames.StackApiKey) { DisplayName = "Stack API key", Sensitive = true },
                new(CredsNames.ManagementToken) { DisplayName = "Management token", Sensitive = true },
                new(CredsNames.BranchName) { DisplayName = "Branch name" }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    { 
        var providers = values.Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value)).ToList();

        var connectionType = values[nameof(ConnectionPropertyGroup)] switch
        {
            var ct when ConnectionType.SupportedConnectionTypes.Contains(ct) => ct,
            _ => throw new Exception($"Unknown connection type: {values[nameof(ConnectionPropertyGroup)]}")
        };

        providers.Add(new AuthenticationCredentialsProvider(CredsNames.ConnectionType, connectionType));
        return providers;
    }
}