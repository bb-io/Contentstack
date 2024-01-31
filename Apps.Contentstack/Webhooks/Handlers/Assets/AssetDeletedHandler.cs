using Apps.Contentstack.Webhooks.Handlers.Base;

namespace Apps.Contentstack.Webhooks.Handlers.Assets;

public class AssetDeletedHandler : ContentstackWebhookHandler
{
    protected override string Event => "assets.delete";
}