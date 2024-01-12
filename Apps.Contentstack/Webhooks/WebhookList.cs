using Apps.Contentstack.Constants;
using Apps.Contentstack.Webhooks.Handlers.Assets;
using Apps.Contentstack.Webhooks.Handlers.Entries;
using Apps.Contentstack.Webhooks.Models;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;

namespace Apps.Contentstack.Webhooks;

[WebhookList]
public class WebhookList
{
    #region Assets

    [Webhook("On asset deleted", typeof(AssetDeletedHandler),
        Description = "On any asset deleted")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetDeleted(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    [Webhook("On asset published", typeof(AssetPublishedHandler),
        Description = "On any asset published")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetPublished(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    [Webhook("On asset unpublished", typeof(AssetUnpublishedHandler),
        Description = "On any asset unpublished")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetUnpublished(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    #endregion

    #region Entries

    [Webhook("On entry created", typeof(EntryCreatedHandler),
        Description = "On any entry created")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryCreated(WebhookRequest webhookRequest)
        => HandleWebhook<EntryWebhookResponse>(webhookRequest);

    [Webhook("On entry deleted", typeof(EntryDeletedHandler),
        Description = "On any entry deleted")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryDeleted(WebhookRequest webhookRequest)
        => HandleWebhook<EntryWebhookResponse>(webhookRequest);

    [Webhook("On entry published", typeof(EntryPublishedHandler),
        Description = "On any entry published")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryPublished(WebhookRequest webhookRequest)
        => HandleWebhook<EntryWebhookResponse>(webhookRequest);

    [Webhook("On entry unpublished", typeof(EntryUnpublishedHandler),
        Description = "On any entry unpublished")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUnpublished(WebhookRequest webhookRequest)
        => HandleWebhook<EntryWebhookResponse>(webhookRequest);

    [Webhook("On entry updated", typeof(EntryUpdatedHandler),
        Description = "On any entry updated")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUpdated(WebhookRequest webhookRequest)
        => HandleWebhook<EntryWebhookResponse>(webhookRequest);

    #endregion

    private Task<WebhookResponse<T>> HandleWebhook<T>(WebhookRequest webhookRequest) where T : class
    {
        var payload = webhookRequest.Body.ToString();
        ArgumentException.ThrowIfNullOrEmpty(payload, nameof(webhookRequest.Body));

        var result = JsonConvert.DeserializeObject<ContentstackWebhookResponse<T>>(payload, JsonConfig.Settings)!;

        return Task.FromResult(new WebhookResponse<T>
        {
            HttpResponseMessage = null,
            Result = result.Data
        });
    }
}