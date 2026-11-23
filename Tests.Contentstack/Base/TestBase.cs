using Microsoft.Extensions.Configuration;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Tests.Contentstack.Base;

public class TestBase
{
    public static List<IEnumerable<AuthenticationCredentialsProvider>> CredentialGroups { get; private set; }
    
    public static List<InvocationContext> InvocationContexts { get; private set; }
    
    public TestContext? TestContext { get; set; }

    public FileManager FileManager { get; set; }
    
    private static string TestFolder { get; }

    static TestBase()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        
        CredentialGroups = config.GetSection("ConnectionDefinition")
            .GetChildren()
            .Select(section =>
                section.GetChildren()
                    .Select(child => new AuthenticationCredentialsProvider(child.Key, child.Value ?? string.Empty))
            )
            .ToList();

        InvocationContexts = CredentialGroups.Select(group => new InvocationContext
        {
            AuthenticationCredentialsProviders = group
        }).ToList();
        
        TestFolder = config["TestFolder"] ?? throw new InvalidOperationException("Set TestFolder in appsettings.json");
    }

    protected TestBase()
    {
        FileManager = new FileManager(TestFolder);
    }
}