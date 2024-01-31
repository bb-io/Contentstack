using Apps.Contentstack.Webhooks.Handlers.Base;

namespace Apps.Contentstack.Webhooks.Handlers.Entries;

public class EntryPublishedHandler : ContentstackWebhookHandler
{
    protected override string Event => "content_types.entries.publish";
}