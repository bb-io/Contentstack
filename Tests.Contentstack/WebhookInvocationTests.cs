using Apps.Contentstack.Webhooks;
using Blackbird.Applications.Sdk.Common.Webhooks;

namespace Tests.Contentstack;

[TestClass]
public class WebhookInvocationTests
{
    [TestMethod]
    public void EntriesWebhookList_IsInvokedAsynchronously()
    {
        Assert.IsTrue(typeof(IAsyncWebhookHandler).IsAssignableFrom(typeof(WebhookList)));
    }

    [TestMethod]
    public void AssetsWebhookList_IsInvokedAsynchronously()
    {
        Assert.IsTrue(typeof(IAsyncWebhookHandler).IsAssignableFrom(typeof(AssetsWebhookList)));
    }
}
