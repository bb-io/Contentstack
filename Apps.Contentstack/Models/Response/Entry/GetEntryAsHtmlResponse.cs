using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;

namespace Apps.Contentstack.Models.Response.Entry;

public record GetEntryAsHtmlResponse(FileReference Content) : IDownloadContentOutput
{
    public FileReference Content { get; set; } = Content;
}
