using Newtonsoft.Json;
using Apps.Contentstack.Constants;
using Apps.Contentstack.Webhooks.Models;
using Apps.Contentstack.Webhooks.Handlers.Assets;
using Blackbird.Applications.Sdk.Common.Webhooks;

namespace Apps.Contentstack.Webhooks;

[WebhookList("Assets")]
public class AssetsWebhookList
{
    [Webhook("On asset deleted", typeof(AssetDeletedHandler), Description = "On any asset deleted")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetDeleted(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    [Webhook("On asset published", typeof(AssetPublishedHandler), Description = "On any asset published")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetPublished(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    [Webhook("On asset unpublished", typeof(AssetUnpublishedHandler), Description = "On any asset unpublished")]
    public Task<WebhookResponse<AssetWebhookResponse>> OnAssetUnpublished(WebhookRequest webhookRequest)
        => HandleWebhook<AssetWebhookResponse>(webhookRequest);

    private static Task<WebhookResponse<T>> HandleWebhook<T>(WebhookRequest webhookRequest) where T : class
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
