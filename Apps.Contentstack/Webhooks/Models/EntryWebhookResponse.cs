using Newtonsoft.Json;
using Apps.Contentstack.Webhooks.Models.Payloads;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Webhooks.Models;

public record EntryWebhookResponse(EntryWebhookPayload payload) : IDownloadContentInput
{
    [Display("Entry ID")]
    [JsonProperty("uid")]
    public string ContentId { get; set; } = payload.Entry.Uid;

    [Display("Entry title")]
    public string EntryTitle { get; set; } = payload.Entry.Title;

    [Display("Entry tags")]
    public IEnumerable<string> Tags { get; set; } = payload.Entry.Tags;

    [Display("Entry created at")]
    public DateTime EntryCreatedAt { get; set; } = payload.Entry.CreatedAt;

    [Display("Content type ID")]
    public string ContentTypeUid { get; set; } = payload.ContentType.Uid;

    [Display("Content type title")]
    public string ContentTypeTitle { get; set; } = payload.ContentType.Title;

    [Display("Content type description")]
    public string ContentTypeDescription { get; set; } = payload.ContentType.Description;

    [Display("Content type created at")]
    public DateTime ContentTypeCreatedAt { get; set; } = payload.ContentType.CreatedAt;
}