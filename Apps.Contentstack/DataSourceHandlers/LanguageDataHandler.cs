using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class LanguageDataHandler : AppInvocable, IAsyncDataSourceHandler
{
    public LanguageDataHandler(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    public async Task<Dictionary<string, string>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/locales", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ListLocalesResponse>(request);
        
        return response.Locales
            .Where(x => context.SearchString is null ||
                        x.Name.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToDictionary(x => x.Code, x => x.Name);
    }
}