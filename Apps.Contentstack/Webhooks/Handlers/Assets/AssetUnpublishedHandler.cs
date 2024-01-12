using Apps.Contentstack.Webhooks.Handlers.Base;

namespace Apps.Contentstack.Webhooks.Handlers.Assets;

public class AssetUnpublishedHandler : ContentstackWebhookHandler
{
    protected override string Event => "assets.unpublish";
}