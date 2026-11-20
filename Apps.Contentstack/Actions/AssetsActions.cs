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
using System.Globalization;

namespace Apps.Contentstack.Actions;

[ActionList("Assets")]
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

    [Action("Publish asset", Description = "Publish a specific asset based on ID")]
    public async Task<NoticeResponse> PublishAsset(
        [ActionParameter] AssetRequest input,
        [ActionParameter] PublishAssetRequest publishRequest)
    {
        var getAssetRequest = new ContentstackRequest($"v3/assets/{input.AssetId}", Method.Get, Creds);
        var asset = await Client.ExecuteWithErrorHandling<AssetResponse>(getAssetRequest);

        var body = new Dictionary<string, object>
        {
            {
                "asset", new
                {
                    environments = new[] { publishRequest.Environment },
                    locales = new[] { publishRequest.Locale }
                }
            },
            { "version", asset.Asset.Version }
        };

        if (publishRequest.ScheduledAt.HasValue)
        {
            body.Add("scheduled_at",
                publishRequest.ScheduledAt.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        var request = new ContentstackRequest($"v3/assets/{input.AssetId}/publish", Method.Post, Creds)
            .AddHeader("api_version", "3.2")
            .WithJsonBody(body);

        return await Client.ExecuteWithErrorHandling<NoticeResponse>(request);
    }
}
