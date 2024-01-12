using Apps.Contentstack.Webhooks.Handlers.Base;

namespace Apps.Contentstack.Webhooks.Handlers.Entries;

public class EntryDeletedHandler : ContentstackWebhookHandler
{
    protected override string Event => "content_types.entries.delete";
}