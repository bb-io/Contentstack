using Apps.Contentstack.Constants;
using Apps.Contentstack.Webhooks.Handlers.Entries;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.Contentstack.Base;

namespace Tests.Contentstack;

[TestClass]
public class WebhookSubscriptionValidationOnlineTests : TestBaseMultipleConnections
{
    [TestMethod, TargetConnections]
    public async Task ValidateSubscription_UrlNeverSubscribed_IsInvalid(InvocationContext invocationContext)
    {
        var handler = new EntryCreatedHandler();
        var values = new Dictionary<string, string> { [CredsNames.WebhookUrl] = $"https://example.com/never-subscribed/{Guid.NewGuid()}" };

        var result = await handler.ValidateSubscription(invocationContext.AuthenticationCredentialsProviders, values);

        TestContext.WriteLine(result.Message);
        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Message, "no longer exists");
    }
}
