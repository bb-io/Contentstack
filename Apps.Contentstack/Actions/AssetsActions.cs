using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Asset;
using Apps.Contentstack.Models.Response;
using Apps.Contentstack.Models.Response.Asset;
using Apps.Contentstack.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Utilities;
using Contentstack.Management.Core.Models;

namespace Apps.Contentstack.Actions;

[ActionList]
public class AssetsActions : AppInvocable
{
    private readonly IFileManagementClient _fileManagementClient;

    public AssetsActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : base(
        invocationContext)
    {
        _fileManagementClient = fileManagementClient;
    }

    [Action("Download asset", Description = "Download content of a specific asset")]
    public async Task<FileResponse> DownloadAsset([ActionParameter] AssetRequest input)
    {
        var response = await ContentstackErrorHandler.HandleRequest(() => Client.Asset(input.AssetId).FetchAsync());
        var asset = Client.ProcessResponse<AssetResponse>(response).Asset;

        var file = await FileDownloader.DownloadFileBytes(asset.Url, _fileManagementClient);
        file.Name = asset.Filename;

        return new(file);
    }

    [Action("Upload asset", Description = "Upload a new asset")]
    public async Task UploadAsset([ActionParameter] UploadAssetRequest input)
    {
        var file = await _fileManagementClient.DownloadAsync(input.File);
        var asset = new AssetModel(input.File.Name, file, input.File.ContentType,
            input.Title, input.Description, input.ParentFolderId);
        
        await ContentstackErrorHandler.HandleRequest(() => Client.Asset().CreateAsync(asset));
    }
}