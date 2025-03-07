using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Webhooks.Handlers.Assets;
using Apps.Contentstack.Webhooks.Handlers.Entries;
using Apps.Contentstack.Webhooks.Models;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryCreated(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry deleted", typeof(EntryDeletedHandler),
        Description = "On any entry deleted")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryDeleted(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry published", typeof(EntryPublishedHandler),
        Description = "On any entry published")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryPublished(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry unpublished", typeof(EntryUnpublishedHandler),
        Description = "On any entry unpublished")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUnpublished(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry updated", typeof(EntryUpdatedHandler),
        Description = "On any entry updated")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUpdated(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    #endregion

    private Task<WebhookResponse<EntryWebhookResponse>> HandleEntryWebhook(WebhookRequest webhookRequest,
        ContentTypeOptionalRequest contentTypeRequest)
    {
        var payload = webhookRequest.Body.ToString();
        ArgumentException.ThrowIfNullOrEmpty(payload, nameof(webhookRequest.Body));

        var result = JsonConvert.DeserializeObject<ContentstackWebhookResponse<EntryWebhookResponse>>(payload, JsonConfig.Settings)!;

        if (!string.IsNullOrEmpty(contentTypeRequest.ContentTypeId))
        {
            if (contentTypeRequest.ContentTypeId != result.Data.ContentType.Uid)
            {
                return Task.FromResult(new WebhookResponse<EntryWebhookResponse>()
                {
                    ReceivedWebhookRequestType = WebhookRequestType.Preflight,
                    Result = null
                });
            }
        }
        if (!string.IsNullOrEmpty(contentTypeRequest.Tag))
        {
            var tags = result.Data.Entry.Tags;
            if (tags == null || !tags.Any(t => t.Trim().Contains(contentTypeRequest.Tag.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(new WebhookResponse<EntryWebhookResponse>
                {
                    ReceivedWebhookRequestType = WebhookRequestType.Preflight,
                    Result = null
                });
            }
        }

        return Task.FromResult(new WebhookResponse<EntryWebhookResponse>
        {
            HttpResponseMessage = null,
            Result = result.Data
        });
    }
    
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