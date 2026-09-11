namespace Apps.Contentstack.Constants;

public static class ConnectionType
{
    public const string ApiKey = "Developer API key";
    public const string BranchScopedApiKey = "Developer API key (branch-scoped)";

    public static readonly string[] SupportedConnectionTypes = [ApiKey, BranchScopedApiKey];
}