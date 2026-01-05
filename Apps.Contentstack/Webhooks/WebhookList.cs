using Newtonsoft.Json;
using Apps.Contentstack.Constants;
using Apps.Contentstack.Webhooks.Models;
using Apps.Contentstack.Webhooks.Handlers.Entries;
using Apps.Contentstack.Webhooks.Models.Payloads;
using Apps.Contentstack.Models.Request.ContentType;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.Sdk.Common.Webhooks;

namespace Apps.Contentstack.Webhooks;

[WebhookList("Entries")]
public class WebhookList
{
    [Webhook("On entry created", typeof(EntryCreatedHandler), Description = "On any entry created")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryCreated(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry deleted", typeof(EntryDeletedHandler), Description = "On any entry deleted")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryDeleted(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdated)]
    [Webhook("On entry published", typeof(EntryPublishedHandler), Description = "On any entry published")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryPublished(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry unpublished", typeof(EntryUnpublishedHandler), Description = "On any entry unpublished")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUnpublished(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    [Webhook("On entry updated", typeof(EntryUpdatedHandler), Description = "On any entry updated")]
    public Task<WebhookResponse<EntryWebhookResponse>> OnEntryUpdated(WebhookRequest webhookRequest,
        [WebhookParameter] ContentTypeOptionalRequest contentTypeRequest)
        => HandleEntryWebhook(webhookRequest, contentTypeRequest);

    private Task<WebhookResponse<EntryWebhookResponse>> HandleEntryWebhook(WebhookRequest webhookRequest,
        ContentTypeOptionalRequest contentTypeRequest)
    {
        var payload = webhookRequest.Body.ToString();
        ArgumentException.ThrowIfNullOrEmpty(payload, nameof(webhookRequest.Body));

        var result = JsonConvert.DeserializeObject<ContentstackWebhookResponse<EntryWebhookPayload>>(payload, JsonConfig.Settings)!;

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
            Result = new EntryWebhookResponse(result.Data)
        });
    }
}