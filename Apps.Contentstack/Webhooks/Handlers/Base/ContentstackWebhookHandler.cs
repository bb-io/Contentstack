using Apps.Contentstack.Api;
using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Request.Webhook;
using Apps.Contentstack.Models.Response.Webhook;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using RestSharp;

namespace Apps.Contentstack.Webhooks.Handlers.Base;

public abstract class ContentstackWebhookHandler : IWebhookEventHandler, IAsyncValidatableWebhookEventHandler
{
    protected abstract string Event { get; }

    protected virtual string? Branch => null;

    public Task SubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
        Dictionary<string, string> values)
    {
        var creds = authenticationCredentialsProvider.ToArray();
        var client = new ContentstackClient(creds);

        var request = new ContentstackRequest("v3/webhooks", Method.Post, creds)
            .WithJsonBody(new CreateWebhookRequest()
            {
                Webhook = new()
                {
                    Name = $"Blackbird-{Event.Replace("content_types.", "")}-{DateTimeOffset.Now:MM/dd/yyyy HH:mm}",
                    Channels = [Event],
                    Destinations =
                    [
                        new WebhookDestinationEntity() { TargetUrl = values[CredsNames.WebhookUrl] }
                    ],
                    Branches = string.IsNullOrWhiteSpace(Branch) ? null : [Branch],
                    RetryPolicy = "manual"
                }
            }, JsonConfig.Settings);

        return client.ExecuteWithErrorHandling(request);
    }

    public async Task UnsubscribeAsync(IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
        Dictionary<string, string> values)
    {
        var creds = authenticationCredentialsProvider.ToArray();
        var client = new ContentstackClient(creds);

        var webhookToDelete = await FindSubscribedWebhook(client, creds, values);

        if (webhookToDelete is null)
            return;

        var request = new ContentstackRequest($"v3/webhooks/{webhookToDelete.Uid}", Method.Delete, creds);
        await client.ExecuteWithErrorHandling(request);
    }

    public async Task<WebhookSubscriptionValidationResponse> ValidateSubscription(
        IEnumerable<AuthenticationCredentialsProvider> authenticationCredentialsProvider,
        Dictionary<string, string> values)
    {
        try
        {
            var creds = authenticationCredentialsProvider.ToArray();
            var client = new ContentstackClient(creds);

            var webhook = await FindSubscribedWebhook(client, creds, values);
            return ValidateWebhook(webhook);
        }
        catch (Exception exception)
        {
            return Invalid($"Could not verify the Contentstack webhook subscription for '{Event}': {exception.Message}");
        }
    }

    internal WebhookSubscriptionValidationResponse ValidateWebhook(WebhookEntity? webhook)
    {
        if (webhook is null)
            return Invalid($"The Contentstack webhook for '{Event}' no longer exists. Republish the Bird to subscribe again.");

        if (webhook.Unhealthy?.State == true)
            return Invalid($"Contentstack marked the webhook '{webhook.Name}' as unhealthy after repeated failed deliveries and stopped sending events. " +
                           "Republish the Bird to subscribe again, or enable the webhook in Contentstack under Settings > Webhooks.");

        if (webhook.Disabled == true)
            return Invalid($"The Contentstack webhook '{webhook.Name}' is disabled. " +
                           "Enable it in Contentstack under Settings > Webhooks, or republish the Bird to subscribe again.");

        if (!(webhook.Channels ?? []).Contains(Event, StringComparer.OrdinalIgnoreCase))
            return Invalid($"The Contentstack webhook '{webhook.Name}' no longer listens to the '{Event}' event. Republish the Bird to subscribe again.");

        return new() { IsValid = true };
    }

    internal static WebhookEntity? FindWebhookByTargetUrl(IEnumerable<WebhookEntity>? webhooks, string targetUrl)
        => (webhooks ?? []).FirstOrDefault(webhook =>
            webhook.Destinations?.Any(destination => destination.TargetUrl == targetUrl) == true);

    private async Task<WebhookEntity?> FindSubscribedWebhook(ContentstackClient client,
        AuthenticationCredentialsProvider[] creds, Dictionary<string, string> values)
    {
        var webhooks = await GetAllWebhooks(client, creds);
        return FindWebhookByTargetUrl(webhooks.Webhooks, values[CredsNames.WebhookUrl]);
    }

    private static WebhookSubscriptionValidationResponse Invalid(string message)
        => new() { IsValid = false, Message = message };

    private Task<WebhooksResponse> GetAllWebhooks(ContentstackClient client, AuthenticationCredentialsProvider[] creds)
    {
        var request = new ContentstackRequest("v3/webhooks", Method.Get, creds);
        return client.ExecuteWithErrorHandling<WebhooksResponse>(request);
    }
}