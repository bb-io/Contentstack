using Apps.Contentstack.Webhooks.Handlers.Base;

namespace Apps.Contentstack.Webhooks.Handlers.Assets;

public class AssetPublishedHandler : ContentstackWebhookHandler
{
    protected override string Event => "assets.publish";
}