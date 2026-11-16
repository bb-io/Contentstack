using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Response.Webhook;
using Apps.Contentstack.Webhooks.Handlers.Base;
using Apps.Contentstack.Webhooks.Handlers.Entries;
using Newtonsoft.Json;

namespace Tests.Contentstack;

[TestClass]
public class WebhookSubscriptionValidationTests
{
    private const string PublishEvent = "content_types.entries.publish";
    private const string BirdUrl = "https://example.com/webhooks/bird-1";

    private static WebhookEntity BuildWebhook(string targetUrl = BirdUrl, bool? disabled = false,
        bool? unhealthy = false, params string[] channels)
        => new()
        {
            Uid = "webhook-1",
            Name = "Blackbird-entries.publish",
            Channels = channels.Length == 0 ? [PublishEvent] : channels,
            Destinations = [new WebhookDestinationEntity { TargetUrl = targetUrl }],
            Disabled = disabled,
            Unhealthy = unhealthy is null ? null : new WebhookHealthEntity { State = unhealthy.Value }
        };

    [TestMethod]
    public void ValidateWebhook_EnabledAndListening_IsValid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(BuildWebhook());

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateWebhook_HealthFieldsMissingFromResponse_IsValid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(BuildWebhook(disabled: null, unhealthy: null));

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateWebhook_WebhookDeleted_IsInvalid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(null);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Message, "no longer exists");
    }

    [TestMethod]
    public void ValidateWebhook_DisabledByContentstackCircuitBreaker_IsInvalid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(BuildWebhook(disabled: true, unhealthy: true));

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Message, "unhealthy");
    }

    [TestMethod]
    public void ValidateWebhook_DisabledManually_IsInvalid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(BuildWebhook(disabled: true));

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Message, "is disabled");
    }

    [TestMethod]
    public void ValidateWebhook_EventRemovedFromChannels_IsInvalid()
    {
        var result = new EntryPublishedHandler(new()).ValidateWebhook(BuildWebhook(channels: "content_types.entries.create"));

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Message, PublishEvent);
    }

    [TestMethod]
    public void FindWebhookByTargetUrl_SeveralBirdsSubscribed_ReturnsOnlyThisBirdsWebhook()
    {
        var otherBird = BuildWebhook("https://example.com/webhooks/bird-10");
        var thisBird = BuildWebhook();
        var withoutDestinations = new WebhookEntity { Uid = "webhook-2", Destinations = null! };

        var result = ContentstackWebhookHandler.FindWebhookByTargetUrl([withoutDestinations, otherBird, thisBird], BirdUrl);

        Assert.AreSame(thisBird, result);
    }

    [TestMethod]
    public void FindWebhookByTargetUrl_NoMatchingDestination_ReturnsNull()
    {
        var result = ContentstackWebhookHandler.FindWebhookByTargetUrl([BuildWebhook("https://example.com/other")], BirdUrl);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void WebhooksResponse_DisabledUnhealthyWebhook_IsDeserialized()
    {
        const string json = """
            {
              "webhooks": [
                {
                  "uid": "webhook-1",
                  "name": "Blackbird-entries.publish",
                  "channels": ["content_types.entries.publish"],
                  "destinations": [{ "target_url": "https://example.com/webhooks/bird-1", "authentication_type": "None" }],
                  "retry_policy": "manual",
                  "disabled": true,
                  "unhealthy": { "state": true },
                  "concise_payload": false
                }
              ]
            }
            """;

        var webhook = JsonConvert.DeserializeObject<WebhooksResponse>(json, JsonConfig.Settings)!.Webhooks.Single();

        Assert.IsTrue(webhook.Disabled);
        Assert.IsTrue(webhook.Unhealthy!.State);
    }

    [TestMethod]
    public void CreateWebhookRequest_HealthFieldsNotSet_AreNotSent()
    {
        var json = JsonConvert.SerializeObject(new WebhookEntity
        {
            Name = "Blackbird-entries.publish",
            Channels = [PublishEvent],
            Destinations = [new WebhookDestinationEntity { TargetUrl = BirdUrl }],
            RetryPolicy = "manual"
        }, JsonConfig.Settings);

        Assert.IsFalse(json.Contains("disabled"));
        Assert.IsFalse(json.Contains("unhealthy"));
    }
}
