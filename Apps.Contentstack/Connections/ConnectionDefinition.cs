﻿using Apps.Contentstack.Constants;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;

namespace Apps.Contentstack.Connections;

public class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => new List<ConnectionPropertyGroup>
    {
        new()
        {
            Name = "Developer API key",
            AuthenticationType = ConnectionAuthenticationType.Undefined,
            ConnectionProperties = new List<ConnectionProperty>
            {
                new(CredsNames.Host) { 
                    DisplayName = "Host region",
                    DataItems =
                    [
                        new ("https://api.contentstack.io", "US (North America, or NA)"),
                        new ("https://eu-api.contentstack.com", "Europe (EU)"),
                        new ("https://azure-na-api.contentstack.com", "Azure North America (Azure NA)"),
                        new ("https://azure-eu-api.contentstack.com", "Azure Europe (Azure EU)")
                    ] 
                },
                new(CredsNames.StackApiKey) { DisplayName = "Stack API key", Sensitive = true },
                new(CredsNames.ManagementToken) { DisplayName = "Management token", Sensitive = true }
            }
        }
    };

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(
        Dictionary<string, string> values) =>
        values.Select(x =>
                new AuthenticationCredentialsProvider(x.Key, x.Value))
            .ToList();
}