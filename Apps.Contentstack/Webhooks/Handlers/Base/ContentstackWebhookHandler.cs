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

public abstract class ContentstackWebhookHandler : IWebhookEventHandler
{
    protected abstract string Event { get; }

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
                    Name = $"Blackbird-{Event}-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                    Channels = new[] { Event },
                    Destinations = new[]
                    {
                        new WebhookDestinationEntity()
                        {
                            TargetUrl = values[CredsNames.WebhookUrl]
                        }
                    },
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

        var webhooks = await GetAllWebhooks(client, creds);
        var webhookToDelete =
            webhooks.Webhooks.FirstOrDefault(x =>
                x.Destinations.Any(x => x.TargetUrl == values[CredsNames.WebhookUrl]));

        if (webhookToDelete is null)
            return;

        var request = new ContentstackRequest($"v3/webhooks/{webhookToDelete.Uid}", Method.Delete, creds);
        await client.ExecuteWithErrorHandling(request);
    }

    private Task<WebhooksResponse> GetAllWebhooks(ContentstackClient client, AuthenticationCredentialsProvider[] creds)
    {
        var request = new ContentstackRequest("v3/webhooks", Method.Get, creds);
        return client.ExecuteWithErrorHandling<WebhooksResponse>(request);
    }
}