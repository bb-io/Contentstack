using Apps.Contentstack.Api;
using Apps.Contentstack.Constants;
using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Property;
using Apps.Contentstack.Models.Request.Workflow;
using Apps.Contentstack.Models.Response;
using Apps.Contentstack.Models.Response.ContentType;
using Apps.Contentstack.Models.Response.Entry;
using Apps.Contentstack.Models.Response.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Utils.Extensions.Files;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Filters.Constants;
using Blackbird.Filters.Extensions;
using Blackbird.Filters.Transformations;
using Blackbird.Filters.Xliff.Xliff1;
using Blackbird.Filters.Xliff.Xliff2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Globalization;
using System.Net.Mime;
using System.Text;
using Apps.Contentstack.Extensions;
using Apps.Contentstack.Helper;

namespace Apps.Contentstack.Actions;

[ActionList("Entries")]
public class EntriesActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AppInvocable(invocationContext)
{
    [Action("Calculate all entries", Description = "Calculate all entries")]
    public async Task<CalculateAllEntriesResponse> CalculateAllEntries([ActionParameter] CalculateEntriesRequest request)
    {
        var contentTypes = await new ContentTypesActions(InvocationContext).ListContentTypes(null);
        var entries = new List<EntryEntity>();

        foreach (var contentType in contentTypes.Items)
        {
            if (request.ContentTypes != null && request.ContentTypes.Any() &&
                !request.ContentTypes.Contains(contentType.Uid))
            {
                continue;
            }

            var result = await SearchEntries(new()
            {
                ContentTypeIds = new[] { contentType.Uid }
            }, new(), new(), new(), new());

            bool isWorkflowStageFilterProvided = request.WorkflowStages != null && request.WorkflowStages.Any();
            if (isWorkflowStageFilterProvided)
            {
                var entriesFiltered = result.Entries.Where(x => request.WorkflowStages.Contains(x.Workflow?.Uid))
                    .ToArray();
                entries.AddRange(entriesFiltered);
            }
            else
            {
                entries.AddRange(result.Entries);
            }
        }

        return new()
        {
            EntriesCount = entries.Count
        };
    }

    [BlueprintActionDefinition(BlueprintAction.SearchContent)]
    [Action("Search entries", Description = "Search for entries based on the provided filters")]
    public async Task<ListEntriesResponse> SearchEntries(
        [ActionParameter] SearchEntriesRequest searchRequest,
        [ActionParameter] WorkflowStageFilterRequest workflowFilter,
        [ActionParameter] LocaleRequest locale,
        [ActionParameter] TagFilterRequest tagFilter,
        [ActionParameter] UpdatedAtFilterRequest updatedAtFilter)
    {
        var contentTypeIds = searchRequest.ContentTypeIds?.ToArray();
        if (contentTypeIds == null || contentTypeIds.Length == 0)
        {
            var contentTypes = await new ContentTypesActions(InvocationContext).ListContentTypes(null);
            contentTypeIds = contentTypes.Items.Select(x => x.Uid).ToArray();
        }

        var allEntries = new List<EntryEntity>();

        foreach (var contentTypeId in contentTypeIds)
        {
            var endpoint = $"v3/content_types/{contentTypeId}/entries"
                .SetQueryParameter("include_workflow", "true");

            if (!string.IsNullOrWhiteSpace(locale.Locale))
                endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

            var request = new ContentstackRequest(endpoint, Method.Get, Creds);
            var result = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(request);
            var entriesForContentType = (result.Entries ?? Enumerable.Empty<EntryEntity>())
                .Select(entry =>
                {
                    entry.ContentTypeId = string.IsNullOrWhiteSpace(entry.ContentTypeId)
                        ? contentTypeId
                        : entry.ContentTypeId;
                    return entry;
                });

            allEntries.AddRange(entriesForContentType);
        }

        var entries = allEntries
            .Where(x => workflowFilter.WorkflowStageId is null || x.Workflow?.Uid == workflowFilter.WorkflowStageId);

        if (!string.IsNullOrWhiteSpace(tagFilter.Tag))
        {
            entries = entries.Where(x => x.Tags != null && x.Tags.Any(t => t.Equals(tagFilter.Tag, StringComparison.OrdinalIgnoreCase)));
        }

        if (updatedAtFilter.UpdatedAfter.HasValue)
        {
            entries = entries.Where(x => x.UpdatedAt >= updatedAtFilter.UpdatedAfter.Value);
        }

        if (updatedAtFilter.UpdatedBefore.HasValue)
        {
            entries = entries.Where(x => x.UpdatedAt <= updatedAtFilter.UpdatedBefore.Value);
        }

        var filteredEntries = entries.ToArray();

        return new ListEntriesResponse
        {
            Entries = filteredEntries,
            Count = filteredEntries.Length
        };
    }

    [Action("Get entry locales", Description = "Get all locales that an entry exists in")]
    public async Task<GetEntryLocalesResponse> GetEntryLocales([ActionParameter] EntryRequest input)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.ContentId}/locales";
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        return await Client.ExecuteWithErrorHandling<GetEntryLocalesResponse>(request);
    }

    [Action("Get entry references", Description = "Get entry IDs and content type IDs where a specific entry is referenced")]
    public async Task<GetEntryReferencesResponse> GetEntryReferences([ActionParameter] EntryRequest input)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.ContentId}/references";
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        return await Client.ExecuteWithErrorHandling<GetEntryReferencesResponse>(request);
    }

    [Action("Get entry", Description = "Get details of a specific entry")]
    public async Task<SingleEntryEntity> GetEntry(
        [ActionParameter] EntryRequest input,
        [ActionParameter] LocaleRequest locale,
        [ActionParameter] FileExtensionRequest fileExtension)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.ContentId}"
            .SetQueryParameter("include_workflow", "true");

        if (!string.IsNullOrWhiteSpace(locale.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling(request);
        var jObject = (JObject)JsonConvert.DeserializeObject(response.Content!)!;
        var entryJObject = jObject["entry"] as JObject;
        var assetIds = ExtractAssetIdsFromJObject(entryJObject, fileExtension.FileExtension);

        var entry = JsonConvert.DeserializeObject<EntryResponse>(response.Content!)!;
        entry.Entry.ContentTypeId = string.IsNullOrWhiteSpace(entry.Entry.ContentTypeId)
            ? input.ContentTypeId
            : entry.Entry.ContentTypeId;
        return new SingleEntryEntity(entry.Entry, assetIds);
    }

    [Action("Publish entry", Description = "Publish a specific entry based on ID")]
    public async Task<NoticeResponse> PublishEntry(
        [ActionParameter] EntryRequest input,
        [ActionParameter] PublishEntryRequest publishEntryRequest)
    {
        var entryEntity = await GetEntry(input, new() { Locale = publishEntryRequest.Locale }, new());

        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.ContentId}/publish"
            .SetQueryParameter("publish_all_localized", "true");

        var dictionaryBody = new Dictionary<string, object>
        {
            {
                "entry", new
                {
                    environments = new[] { publishEntryRequest.Environment },
                    locales = new[] { publishEntryRequest.Locale }
                }
            },
            { "locale", publishEntryRequest.Locale },
            { "version", entryEntity.Version }
        };

        if (publishEntryRequest.ScheduledAt.HasValue)
        {
            dictionaryBody.Add("scheduled_at",
                publishEntryRequest.ScheduledAt.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        var request = new ContentstackRequest(endpoint, Method.Post, Creds)
            .WithJsonBody(dictionaryBody);

        return await Client.ExecuteWithErrorHandling<NoticeResponse>(request);
    }

    [Action("Set entry workflow stage", Description = "Set different workflow stage for a specific entry")]
    public Task SetEntryWorkflowStage(
        [ActionParameter] EntryRequest entry,
        [ActionParameter] WorkflowStageRequest input)
    {
        var endpoint = $"v3/content_types/{entry.ContentTypeId}/entries/{entry.ContentId}/workflow";
        var request = new ContentstackRequest(endpoint, Method.Post, Creds)
            .WithJsonBody(new
            {
                workflow = new
                {
                    workflow_stage = new
                    {
                        uid = input.WorkflowStageId
                    }
                }
            });

        return Client.ExecuteWithErrorHandling(request);
    }

    [Action("Add tag to entry", Description = "Add tag to specific entry")]
    public async Task<NoticeResponse> AddTagToEntry(
    [ActionParameter] EntryRequest input,
    [ActionParameter, Display("Tag")] string tag,
    [ActionParameter] LocaleRequest locale)
    {
        var entryObject = await GetEntryJObject(input.ContentTypeId, input.ContentId, locale.Locale);

        JArray tagsArray;
        if (entryObject["tags"] != null && entryObject["tags"].Type == JTokenType.Array)
        {
            tagsArray = (JArray)entryObject["tags"];
        }
        else
        {
            tagsArray = new JArray();
        }

        if (!tagsArray.Any(t => t.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase)))
        {
            tagsArray.Add(tag);
        }

        entryObject["tags"] = tagsArray;

        await UpdateEntry(input.ContentTypeId, input.ContentId, entryObject, locale.Locale);

        return new NoticeResponse { Notice = "Tag added successfully" };
    }

    [Action("Remove tag from entry", Description = "Remove tag from specific entry")]
    public async Task<NoticeResponse> RemoveTagFromEntry(
    [ActionParameter] EntryRequest input,
    [ActionParameter, Display("Tag")][DataSource(typeof(EntryTagDataSourceHandler))] string tag,
    [ActionParameter] LocaleRequest locale)
    {
        var entryObject = await GetEntryJObject(input.ContentTypeId, input.ContentId, locale.Locale);

        if (entryObject["tags"] != null && entryObject["tags"].Type == JTokenType.Array)
        {
            var tagsArray = (JArray)entryObject["tags"];

            var tagsToRemove = tagsArray
                .Where(t => t.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (tagsToRemove.Any())
            {
                foreach (var token in tagsToRemove)
                {
                    token.Remove();
                }

                await UpdateEntry(input.ContentTypeId, input.ContentId, entryObject, locale.Locale);
                return new NoticeResponse { Notice = "Tag removed successfully" };
            }
            else
            {
                return new NoticeResponse { Notice = "Tag not found in entry" };
            }
        }
        else
        {
            return new NoticeResponse { Notice = "No tags exist in entry" };
        }
    }

    #region Get property

    [Action("Get entry string property", Description = "Get data of a specific entry string property")]
    public async Task<StringPropertyResponse> GetEntryStringProp(
        [ActionParameter] EntryStringPropRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property).ToString()
        };
    }

    [Action("Get entry number property", Description = "Get data of a specific entry number property")]
    public async Task<NumberPropertyResponse> GetEntryNumberProp(
        [ActionParameter] EntryNumberPropRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property)
                .ToObject<decimal>()
        };
    }

    [Action("Get entry date property", Description = "Get data of a specific entry date property")]
    public async Task<DatePropertyResponse> GetEntryDateProp(
        [ActionParameter] EntryDatePropRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property)
                .ToObject<DateTime>()
        };
    }

    [Action("Get entry boolean property", Description = "Get data of a specific entry boolean property")]
    public async Task<BooleanPropertyResponse> GetEntryBooleanProp(
        [ActionParameter] EntryBooleanPropRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property)
                .ToObject<bool>()
        };
    }

    #endregion

    #region Set property

    [Action("Set entry string property", Description = "Set data of a specific entry string property")]
    public Task SetEntryStringProp(
        [ActionParameter] EntryStringPropRequest input,
        [ActionParameter] [Display("Value")] string value,
        [ActionParameter] LocaleRequest locale)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value, locale.Locale);

    [Action("Set entry number property", Description = "Set data of a specific entry number property")]
    public Task SetEntryNumberProp(
        [ActionParameter] EntryNumberPropRequest input,
        [ActionParameter] [Display("Value")] decimal value,
        [ActionParameter] LocaleRequest locale)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value, locale.Locale);

    [Action("Set entry date property", Description = "Set data of a specific entry date property")]
    public Task SetEntryDateProp(
        [ActionParameter] EntryDatePropRequest input,
        [ActionParameter] [Display("Value")] DateTime value,
        [ActionParameter] LocaleRequest locale)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value, locale.Locale);

    [Action("Set entry boolean property", Description = "Set data of a specific entry boolean property")]
    public Task SetEntryBooleanProp(
        [ActionParameter] EntryBooleanPropRequest input,
        [ActionParameter] [Display("Value")] bool value,
        [ActionParameter] LocaleRequest locale)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value, locale.Locale);

    #endregion

    #region HTML conversion

    [BlueprintActionDefinition(BlueprintAction.DownloadContent)]
    [Action("Download entry content", Description = "Download content of a specific entry as HTML file")]
    public async Task<DownloadEntryResponse> GetEntryAsHtml(
        [ActionParameter] DownloadEntryRequest input, 
        [ActionParameter] LocaleRequest locale)
    {
        input.Validate();

        var contentType = await GetContentType(input.ContentTypeId);
        var entry = await GetEntryJObject(input.ContentTypeId, input.ContentId, locale.Locale);

        var updatedByUserId = entry["updated_by"]?.ToString();
        UserEntity? updatedByUser = null;

        if (updatedByUserId != null)
        {
            var userRequest = new ContentstackRequest($"v3/stacks/users/", Method.Get, Creds);
            var allUsers = await Client.ExecuteWithErrorHandling<UsersResponse>(userRequest);
            updatedByUser = allUsers.Users.Find(x => x.Id == updatedByUserId);
        }
        

        Console.WriteLine(entry.ToString());

        IEnumerable<ReferencedEntryData>? referencedData = null;

        if (input.IncludeReferencedEntryContent)
        {
            var referenced = ExtractReferencedEntriesWithContentTypes(entry, contentType);
            var list = new List<ReferencedEntryData>();

            foreach (var (refEntryUid, refContentTypeUid) in referenced)
            {
                try
                {
                    var refSchema = await GetContentType(refContentTypeUid);
                    var refEntry = await GetEntryJObject(refContentTypeUid, refEntryUid, locale.Locale);
                    list.Add(new ReferencedEntryData(refContentTypeUid, refEntryUid, refEntry, refSchema));
                }
                catch
                {
                    // If we can't fetch a referenced entry, skip it silently
                }
            }

            referencedData = list;
        }

        var html = JsonToHtmlConverter.ToHtml(
            entry,
            contentType,
            InvocationContext.Logger,
            input.ContentTypeId,
            input.ContentId,
            Creds.Get(CredsNames.StackApiKey).Value,
            updatedByUser,
            input.ExcludeFieldIds,
            referencedData
        );

        var entryTitle = entry["title"]?.ToString() ?? input.ContentId;
        if (!string.IsNullOrEmpty(locale.Locale)) 
            entryTitle = $"{entryTitle}_{locale.Locale}";
        
        var file = await fileManagementClient.UploadAsync(
            new MemoryStream(html), 
            MediaTypeNames.Text.Html, 
            $"{entryTitle}.html"
        );

        var response = new DownloadEntryResponse(file);

        if (input.IncludeReferencedEntryUids)
        {
            response.ReferencedEntryUids = ExtractReferencedEntryUids(entry, contentType);
            response.ReferencedEntries = ExtractReferencedEntriesWithContentTypes(entry, contentType)
                .Select(x => new EntryReferenceItem
                {
                    EntryId = x.EntryUid,
                    ContentTypeId = x.ContentTypeUid
                })
                .ToList();
        }

        return response;
    }

    [BlueprintActionDefinition(BlueprintAction.UploadContent)]
    [Action("Upload entry content", Description = "Update content of a specific entry from a file")]
    public async Task<UploadEntryResponse> UpdateEntryFromHtml([ActionParameter] UploadEntryRequest input)
    {
        using var memoryStream = new MemoryStream();
        var file = await fileManagementClient.DownloadAsync(input.Content);
        var originalBytes = await file.GetByteData(); 

        var html = Encoding.UTF8.GetString(originalBytes);
        bool isXliff = Xliff2Serializer.IsXliff2(html) || Xliff1Serializer.IsXliff1(html);
        Transformation? transformation = null;
        if (isXliff)
        {
            transformation = Transformation.Parse(html, input.Content.Name);
            var transformedHtml = transformation.Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain files");

            await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(transformedHtml));
        }
        else
            await memoryStream.WriteAsync(originalBytes);

        memoryStream.Position = 0;

        var (extractedContentTypeId, extractedEntryId) = HtmlToJsonConverter.ExtractContentTypeAndEntryId(memoryStream);

        var contentTypeId = input.ContentTypeId ?? extractedContentTypeId ??
            throw new PluginMisconfigurationException("Content type ID is missing. Please provide it as an input or in the HTML file meta tag");
        var entryId = input.ContentId ?? extractedEntryId ??
            throw new PluginMisconfigurationException("Entry ID is missing. Please provide it as an input or in the HTML file meta tag");

        var entry = await GetEntryJObject(contentTypeId, entryId, input.Locale);
        HtmlToJsonConverter.UpdateEntryFromHtml(memoryStream, entry, InvocationContext.Logger);

        await UpdateEntry(contentTypeId, entryId, entry, input.Locale);

        var errors = new List<string>();

        if (!isXliff)
        {
            memoryStream.Position = 0;
            var referencedEntryIds = HtmlToJsonConverter.ExtractReferencedEntryIds(memoryStream);

            foreach (var (refContentTypeId, refEntryId) in referencedEntryIds)
            {
                try
                {
                    var refEntry = await GetEntryJObject(refContentTypeId, refEntryId, input.Locale);
                    memoryStream.Position = 0;
                    HtmlToJsonConverter.UpdateReferencedEntryFromHtml(memoryStream, refContentTypeId, refEntryId, refEntry, InvocationContext.Logger);
                    await UpdateEntry(refContentTypeId, refEntryId, refEntry, input.Locale);
                }
                catch (Exception ex)
                {
                    errors.Add($"Entry {refEntryId} (content type: {refContentTypeId}) could not be updated — {ex.Message}");
                }
            }
        }

        var result = new UploadEntryResponse
        {
            ContentTypeId = contentTypeId,
            EntryId = entryId,
            Errors = errors.Count > 0 ? errors : null
        };

        if (transformation is not null)
        {
            var updatedEntry = await GetEntry(
                new EntryRequest { ContentId = entryId, ContentTypeId = contentTypeId }, 
                new LocaleRequest { Locale = input.Locale},
                new ());
            var stackApiKey = Creds.Get(CredsNames.StackApiKey).Value;
            transformation.TargetSystemReference.ContentId = entryId;
            transformation.TargetSystemReference.ContentName = updatedEntry.Title;
            transformation.TargetSystemReference.AdminUrl = $"https://app.contentstack.com/#!/stack/{stackApiKey}/content-type/{contentTypeId}/{input.Locale}/entry/{entryId}/edit";
            transformation.TargetSystemReference.SystemName = "Contentstack";
            transformation.TargetSystemReference.SystemRef = "https://www.contentstack.com";
            transformation.TargetLanguage = input.Locale;

            result.Content = await fileManagementClient.UploadAsync(transformation.Serialize().ToStream(), MediaTypes.Xliff, transformation.XliffFileName);
        }
        else
        {
            result.Content = input.Content;
        }

        return result;
    }

    [Action("Get entry file metadata", Description = "Extract content type and entry IDs from a file")]
    public async Task<GetEntryFileMetadataResponse> ExtractContentTypeAndEntryId([ActionParameter] FileRequest fileRequest)
    {
        using var memoryStream = new MemoryStream();
        var file = await fileManagementClient.DownloadAsync(fileRequest.Content);
        var originalBytes = await file.GetByteData();

        var html = Encoding.UTF8.GetString(originalBytes);
        if (Xliff2Serializer.IsXliff2(html))
        {
            var transformedHtml = Transformation.Parse(html, "entry.xlf").Target().Serialize()
                ?? throw new PluginMisconfigurationException("XLIFF did not contain files");

            await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(transformedHtml));
        }
        else
            await memoryStream.WriteAsync(originalBytes);

        memoryStream.Position = 0;

        var (contentTypeId, entryId) = HtmlToJsonConverter.ExtractContentTypeAndEntryId(memoryStream);
        return new()
        {
            ContentTypeId = contentTypeId ?? string.Empty,
            EntryId = entryId ?? string.Empty
        };
    }

    #endregion
    
    [Action("Replace entry assets", Description = "Replace referenced entry assets by matching filename substrings")]
    public async Task ReplaceEntryAssets(
        [ActionParameter] EntryRequest entryInput,
        [ActionParameter] ReplaceEntryAssetsRequest replaceInput)
    {
        var entry = await GetEntryJObject(entryInput.ContentTypeId, entryInput.ContentId);
        var assetObjects = entry.Descendants()
            .OfType<JObject>()
            .Where(x => x.IsAssetObject())
            .Select(node => (Node: node, Asset: node.ToObject<AssetEntity>()!))
            .ToList();
        
        var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toReplace = new List<(JObject AssetObject, string TargetName)>();

        string search = replaceInput.ReplaceAssetsContaining;
        string replacement = replaceInput.WithAssetsContaining;
        
        foreach (var (node, asset) in assetObjects)
        {
            string? sourceName = asset.Filename;
            if (string.IsNullOrEmpty(sourceName) || !sourceName.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            string targetName = sourceName.Replace(search, replacement, StringComparison.OrdinalIgnoreCase);
            toReplace.Add((node, targetName));
            targetNames.Add(targetName);
        }

        if (toReplace.Count == 0)
            return;

        var assetHelper = new AssetHelper(InvocationContext);
        var foundAssets = await assetHelper.FindAssetsByNames(targetNames);
        
        if (foundAssets.Count == 0)
        {
            throw new PluginMisconfigurationException(
                $"No replacement assets found. Looked for: {string.Join(", ", targetNames)}. " + 
                "Make sure assets with these names exist or check your 'replace'/'with' inputs.");
        }

        foreach (var (node, asset) in assetObjects)
        {
            string sourceName = asset.Filename;
            string newUid = asset.Uid;

            if (!string.IsNullOrEmpty(sourceName) && sourceName.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                var targetName = sourceName.Replace(search, replacement, StringComparison.OrdinalIgnoreCase);
                if (foundAssets.TryGetValue(targetName, out var newAsset))
                    newUid = newAsset.Uid;
            }

            node.Replace(new JValue(newUid));
        }

        await assetHelper.UpdateEntryWithAssets(entryInput.ContentTypeId, entryInput.ContentId, entry);
    }

    #region Utils

    private static List<string> ExtractAssetIdsFromJObject(JObject entryJObject, string? fileExtension)
    {
        var assetIds = new List<string>();
        foreach (var property in entryJObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object)
            {
                var potentialAsset = property.Value as JObject;
                var uid = potentialAsset?["uid"]?.ToString();
                var fileName = potentialAsset?["filename"]?.ToString();
                if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(fileName))
                {
                    if (string.IsNullOrEmpty(fileExtension) || fileName.EndsWith(fileExtension))
                    {
                        assetIds.Add(uid);
                    }
                }
            }
        }

        return assetIds;
    }

    private static List<string> ExtractReferencedEntryUids(JObject entry, ContentTypeBlockEntity contentType)
    {
        var referencedEntryUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ExtractReferencedEntryUidsFromSchema(entry, contentType.Schema, referencedEntryUids);
        return referencedEntryUids.ToList();
    }

    private static void ExtractReferencedEntryUidsFromSchema(JObject entry, JArray schema, ISet<string> referencedEntryUids)
    {
        foreach (var schemaToken in schema.OfType<JObject>())
        {
            var field = schemaToken.ToObject<EntryProperty>();
            var fieldUid = schemaToken["uid"]?.ToString();

            if (field is null || string.IsNullOrWhiteSpace(fieldUid))
                continue;

            field.Uid = fieldUid;
            var property = entry[fieldUid];
            if (property is null)
                continue;

            ExtractReferencedEntryUidsFromProperty(property, field, referencedEntryUids);
        }
    }

    private static void ExtractReferencedEntryUidsFromProperty(JToken property, EntryProperty field, ISet<string> referencedEntryUids)
    {
        switch (field.DataType)
        {
            case "reference":
                AddReferenceUids(property, referencedEntryUids);
                break;
            case "group":
            case "global_field":
                if (field.Schema is null)
                    break;

                if (property is JObject propertyObject)
                {
                    ExtractReferencedEntryUidsFromSchema(propertyObject, field.Schema, referencedEntryUids);
                }
                else if (property is JArray propertyArray)
                {
                    foreach (var item in propertyArray.OfType<JObject>())
                    {
                        ExtractReferencedEntryUidsFromSchema(item, field.Schema, referencedEntryUids);
                    }
                }
                break;
            case "blocks":
                if (field.Blocks is null || property is not JArray blocksArray)
                    break;

                foreach (var blockItem in blocksArray.OfType<JObject>())
                {
                    var blockProperty = blockItem.Properties().FirstOrDefault();
                    if (blockProperty?.Value is not JObject blockValue)
                        continue;

                    var blockSchema = field.Blocks.FirstOrDefault(x => x.Uid == blockProperty.Name)?.Schema;
                    if (blockSchema is not null)
                    {
                        ExtractReferencedEntryUidsFromSchema(blockValue, blockSchema, referencedEntryUids);
                    }
                }
                break;
        }
    }

    private static void AddReferenceUids(JToken property, ISet<string> referencedEntryUids)
    {
        if (property is JArray propertyArray)
        {
            foreach (var item in propertyArray)
            {
                AddReferenceUid(item, referencedEntryUids);
            }

            return;
        }

        AddReferenceUid(property, referencedEntryUids);
    }

    private static void AddReferenceUid(JToken referenceToken, ISet<string> referencedEntryUids)
    {
        var uid = referenceToken.Type switch
        {
            JTokenType.String => referenceToken.ToString(),
            JTokenType.Object => referenceToken["uid"]?.ToString() ?? referenceToken["entry_uid"]?.ToString(),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(uid))
        {
            referencedEntryUids.Add(uid);
        }
    }

    private static HashSet<(string EntryUid, string ContentTypeUid)> ExtractReferencedEntriesWithContentTypes(JObject entry, ContentTypeBlockEntity contentType)
    {
        var results = new HashSet<(string, string)>();
        ExtractReferencedEntriesFromSchema(entry, contentType.Schema, results);
        return results;
    }

    private static void ExtractReferencedEntriesFromSchema(JObject entry, JArray schema, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        foreach (var schemaToken in schema.OfType<JObject>())
        {
            var field = schemaToken.ToObject<EntryProperty>();
            var fieldUid = schemaToken["uid"]?.ToString();

            if (field is null || string.IsNullOrWhiteSpace(fieldUid))
                continue;

            field.Uid = fieldUid;
            var property = entry[fieldUid];
            if (property is null)
                continue;

            ExtractReferencedEntriesFromProperty(property, field, results);
        }
    }

    private static void ExtractReferencedEntriesFromProperty(JToken property, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        switch (field.DataType)
        {
            case "reference":
                AddReferencesWithContentTypes(property, field, results);
                break;
            case "group":
            case "global_field":
                if (field.Schema is null)
                    break;

                if (property is JObject propertyObject)
                    ExtractReferencedEntriesFromSchema(propertyObject, field.Schema, results);
                else if (property is JArray propertyArray)
                    foreach (var item in propertyArray.OfType<JObject>())
                        ExtractReferencedEntriesFromSchema(item, field.Schema, results);
                break;
            case "blocks":
                if (field.Blocks is null || property is not JArray blocksArray)
                    break;

                foreach (var blockItem in blocksArray.OfType<JObject>())
                {
                    var blockProperty = blockItem.Properties().FirstOrDefault();
                    if (blockProperty?.Value is not JObject blockValue)
                        continue;

                    var blockSchema = field.Blocks.FirstOrDefault(x => x.Uid == blockProperty.Name)?.Schema;
                    if (blockSchema is not null)
                        ExtractReferencedEntriesFromSchema(blockValue, blockSchema, results);
                }
                break;
        }
    }

    private static void AddReferencesWithContentTypes(JToken property, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        if (property is JArray propertyArray)
        {
            foreach (var item in propertyArray)
                AddReferenceWithContentType(item, field, results);
            return;
        }

        AddReferenceWithContentType(property, field, results);
    }

    private static void AddReferenceWithContentType(JToken referenceToken, EntryProperty field, ISet<(string EntryUid, string ContentTypeUid)> results)
    {
        var uid = referenceToken.Type switch
        {
            JTokenType.String => referenceToken.ToString(),
            JTokenType.Object => referenceToken["uid"]?.ToString() ?? referenceToken["entry_uid"]?.ToString(),
            _ => null
        };

        if (string.IsNullOrWhiteSpace(uid))
            return;

        var contentTypeUid = referenceToken.Type == JTokenType.Object
            ? referenceToken["_content_type_uid"]?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(contentTypeUid))
        {
            var fallbackContentTypeIds = GetReferenceContentTypeIds(field).ToList();
            if (fallbackContentTypeIds.Count == 1)
                contentTypeUid = fallbackContentTypeIds[0];
        }

        if (!string.IsNullOrWhiteSpace(uid) && !string.IsNullOrWhiteSpace(contentTypeUid))
            results.Add((uid, contentTypeUid));
    }

    private static IEnumerable<string> GetReferenceContentTypeIds(EntryProperty field)
    {
        if (field.ReferenceTo is null)
            return [];

        return field.ReferenceTo.Type switch
        {
            JTokenType.String => [field.ReferenceTo.ToString()],
            JTokenType.Array => field.ReferenceTo.Values<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase),
            _ => []
        };
    }

    private async Task SetEntryProperty<T>(string contentTypeId, string entryId, string property, T value,
        string? locale = default)
    {
        var entryObject = await GetEntryJObject(contentTypeId, entryId);

        var propertyValue = entryObject.Descendants()
            .First(x => x.Parent is JProperty prop && prop.Name == property) as JValue;
        propertyValue!.Value = value;

        await UpdateEntry(contentTypeId, entryId, entryObject, locale);
    }

    private async Task UpdateEntry(string contentTypeId, string entryId, JObject entryObject, string? locale = default)
    {
        var contentTypeObj = await GetContentType(contentTypeId);

        var fileProps = contentTypeObj.Schema
            .Descendants()
            .Where(x => x is JObject && x["data_type"]?.ToString() == "file")
            .Select(x => x["uid"]!.ToString())
            .ToList();

        fileProps.ForEach(x => RemovePropertyByName(entryObject, x));

        var endpoint = $"v3/content_types/{contentTypeId}/entries/{entryId}";
        if (!string.IsNullOrWhiteSpace(locale))
        {
            endpoint = endpoint.SetQueryParameter("locale", locale);
        }

        var request = new ContentstackRequest(endpoint, Method.Put, Creds)
            .WithJsonBody(new
            {
                entry = entryObject
            });

        try
        {
            await Client.ExecuteWithErrorHandling(request);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("is not a valid enum value for") || ex.Message.Contains("Language was not found"))
            {
                throw new PluginApplicationException(ex.Message);
            }

            throw new PluginApplicationException(
                $"Entry update failed. Exception: {ex}; Exception type: {ex.GetType().Name}; Content type schema: {contentTypeObj.Schema}; Entry JSON: {entryObject};");
        }
    }

    private async Task<JObject> GetEntryJObject(string contentTypeId, string entryId, string? locale = default)
    {
        var endpoint = $"v3/content_types/{contentTypeId}/entries/{entryId}";

        if (!string.IsNullOrWhiteSpace(locale))
            endpoint = endpoint.SetQueryParameter("locale", locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<EntryGenericResponse>(request);

        return response.Entry;
    }

    private async Task<ContentTypeBlockEntity> GetContentType(string contentTypeId)
    {
        var endpoint = $"v3/content_types/{contentTypeId}"
            .SetQueryParameter("include_global_field_schema", "true");
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<ContentTypeResponse>(request);

        return response.ContentType;
    }

    private static void RemovePropertyByName(JToken token, string propertyName)
    {
        if (token.Type is JTokenType.Property or JTokenType.Array)
        {
            foreach (var child in token.Children())
                RemovePropertyByName(child, propertyName);

            return;
        }

        if (token is not JObject obj)
            return;

        obj.Property(propertyName)?.Remove();

        foreach (var child in obj.Children())
            RemovePropertyByName(child, propertyName);
    }

    #endregion
}
