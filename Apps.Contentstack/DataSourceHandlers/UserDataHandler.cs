using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.DataSourceHandlers;

public class UserDataHandler(InvocationContext invocationContext)
    : AppInvocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context,
        CancellationToken cancellationToken)
    {
        var request = new ContentstackRequest("v3/stacks/users", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<UsersResponse>(request);

        return (response.Users ?? [])
            .Select(x => new DataSourceItem(x.Id, GetDisplayName(x)))
            .Where(x => context.SearchString is null ||
                        x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetDisplayName(UserEntity user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var name = string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;

        if (string.IsNullOrWhiteSpace(name))
            return user.Email ?? user.Id;

        return string.IsNullOrWhiteSpace(user.Email) ? name : $"{name} ({user.Email})";
    }
}
