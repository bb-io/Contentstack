using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Asset;
using Apps.Contentstack.Models.Response;
using Apps.Contentstack.Models.Response.Asset;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Utilities;
using Contentstack.Management.Core.Models;

namespace Apps.Contentstack.Actions;

[ActionList]
public class AssetsActions : AppInvocable
{
    public AssetsActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("Download asset", Description = "Download content of a specific asset")]
    public async Task<FileResponse> DownloadAsset([ActionParameter] AssetRequest input)
    {
        var response = await Client.Asset(input.AssetId).FetchAsync();
        var asset = Client.ProcessResponse<AssetResponse>(response).Asset;

        var file = await FileDownloader.DownloadFileBytes(asset.Url);
        file.Name = asset.Filename;

        return new(file);
    }

    [Action("Upload asset", Description = "Upload a new asset")]
    public Task UploadAsset([ActionParameter] UploadAssetRequest input)
    {
        var asset = new AssetModel(input.File.Name, new MemoryStream(input.File.Bytes), input.File.ContentType,
            input.Title, input.Description, input.ParentFolderId);
        return Client.Asset().CreateAsync(asset);
    }
}