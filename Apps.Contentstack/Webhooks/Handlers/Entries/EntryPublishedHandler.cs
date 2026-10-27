using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Webhooks.Handlers.Base;
using Blackbird.Applications.Sdk.Common.Webhooks;

namespace Apps.Contentstack.Webhooks.Handlers.Entries;

public class EntryPublishedHandler(
    [WebhookParameter] EntryPublishedOptionalRequest request) : ContentstackWebhookHandler
{
    protected override string Event => "content_types.entries.publish";

    protected override string? Branch => request.Branch;
}