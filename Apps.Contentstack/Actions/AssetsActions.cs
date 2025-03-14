using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Asset;
using Apps.Contentstack.Models.Response;
using Apps.Contentstack.Models.Response.Asset;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.System;
using Blackbird.Applications.Sdk.Utils.Utilities;
using RestSharp;

namespace Apps.Contentstack.Actions;

[ActionList]
public class AssetsActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : AppInvocable(
    invocationContext)
{
    [Action("Download asset", Description = "Download content of a specific asset")]
    public async Task<FileResponse> DownloadAsset([ActionParameter] AssetRequest input)
    {
        var request = new ContentstackRequest($"v3/assets/{input.AssetId}", Method.Get, Creds);
        var asset = await Client.ExecuteWithErrorHandling<AssetResponse>(request);

        var file = await FileDownloader.DownloadFileBytes(asset.Asset.Url, fileManagementClient);
        file.Name = asset.Asset.Filename;

        return new(file);
    }

    [Action("Upload asset", Description = "Upload a new asset")]
    public async Task UploadAsset([ActionParameter] UploadAssetRequest input)
    {
        var file = await fileManagementClient.DownloadAsync(input.File);

        var formData = new Dictionary<string, string>()
        {
            ["asset[title]"] = input.Title!,
            ["asset[description]"] = input.Description!,
            ["asset[parent_uid]"] = input.ParentFolderId!,
        }.AllIsNotNull();
        var request = new ContentstackRequest("v3/assets", Method.Post, Creds);
        formData.ToList().ForEach(x => request.AddParameter(x.Key, x.Value));

        request.AddFile("asset[upload]", () => file, input.File.Name!);
        request.AlwaysMultipartFormData = true;

        await Client.ExecuteWithErrorHandling(request);
    }
}