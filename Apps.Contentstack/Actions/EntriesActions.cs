using System.Globalization;
using System.Net.Mime;
using Apps.Contentstack.Api;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Property;
using Apps.Contentstack.Models.Request.Workflow;
using Apps.Contentstack.Models.Response;
using Apps.Contentstack.Models.Response.ContentType;
using Apps.Contentstack.Models.Response.Entry;
using Apps.Contentstack.Models.Response.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Contentstack.Actions;

[ActionList]
public class EntriesActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient)
    : AppInvocable(invocationContext)
{
    [Action("Calculate all entries", Description = "Calculate all entries")]
    public async Task<CalculateAllEntriesResponse> CalculateAllEntries(
        [ActionParameter] CalculateEntriesRequest request)
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
                ContentTypeId = contentType.Uid
            }, new(), new(),new());

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

    [Action("Search entries", Description = "Search for entries based on the provided filters")]
    public async Task<ListEntriesResponse> SearchEntries(
        [ActionParameter] ContentTypeRequest contentType,
        [ActionParameter] WorkflowStageFilterRequest workflowFilter,
        [ActionParameter] LocaleRequest locale,
        [ActionParameter] TagFilterRequest tagFilter)
    {
        var endpoint = $"v3/content_types/{contentType.ContentTypeId}/entries"
        .SetQueryParameter("include_workflow", "true");

        if (!string.IsNullOrWhiteSpace(locale.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var result = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(request);

        var entries = result.Entries
            .Where(x => workflowFilter.WorkflowStageId is null || x.Workflow?.Uid == workflowFilter.WorkflowStageId);

        if (!string.IsNullOrWhiteSpace(tagFilter.Tag))
        {
            entries = entries.Where(x => x.Tags != null && x.Tags.Any(t => t.Equals(tagFilter.Tag, StringComparison.OrdinalIgnoreCase)));
        }

        var filteredEntries = entries.ToArray();

        return new ListEntriesResponse
        {
            Entries = filteredEntries,
            Count = filteredEntries.Length
        };
    }

    [Action("Get entry", Description = "Get details of a specific entry")]
    public async Task<SingleEntryEntity> GetEntry(
        [ActionParameter] EntryRequest input,
        [ActionParameter] LocaleRequest locale,
        [ActionParameter] FileExtensionRequest fileExtension)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.EntryId}"
            .SetQueryParameter("include_workflow", "true");

        if (!string.IsNullOrWhiteSpace(locale.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling(request);
        var jObject = (JObject)JsonConvert.DeserializeObject(response.Content!)!;
        var entryJObject = jObject["entry"] as JObject;
        var assetIds = ExtractAssetIdsFromJObject(entryJObject, fileExtension.FileExtension);

        var entry = JsonConvert.DeserializeObject<EntryResponse>(response.Content!)!;
        return new SingleEntryEntity(entry.Entry, assetIds);
    }

    [Action("Publish entry", Description = "Publish a specific entry based on ID")]
    public async Task<NoticeResponse> PublishEntry(
        [ActionParameter] EntryRequest input,
        [ActionParameter] PublishEntryRequest publishEntryRequest)
    {
        var entryEntity = await GetEntry(input, new() { Locale = publishEntryRequest.Locale }, new());

        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.EntryId}/publish"
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
        var endpoint = $"v3/content_types/{entry.ContentTypeId}/entries/{entry.EntryId}/workflow";
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
        var entryObject = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

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

        await UpdateEntry(input.ContentTypeId, input.EntryId, entryObject, locale.Locale);

        return new NoticeResponse { Notice = "Tag added successfully" };
    }

    [Action("Remove tag from entry", Description = "Remove tag from specific entry")]
    public async Task<NoticeResponse> RemoveTagFromEntry(
    [ActionParameter] EntryRequest input,
    [ActionParameter, Display("Tag")] string tag,
    [ActionParameter] LocaleRequest locale)
    {
        var entryObject = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);

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

                await UpdateEntry(input.ContentTypeId, input.EntryId, entryObject, locale.Locale);
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

    [Action("Get entry content as HTML", Description = "Get content of a specific entry as HTML file")]
    public async Task<FileResponse> GetEntryAsHtml(
        [ActionParameter] EntryRequest input, 
        [ActionParameter] LocaleRequest locale)
    {
        var contentType = await GetContentType(input.ContentTypeId);

        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);
        var html = JsonToHtmlConverter.ToHtml(entry, contentType, InvocationContext.Logger, input.ContentTypeId,
            input.EntryId);

        var entryTitle = entry["title"]?.ToString() ?? input.EntryId;
        if(!string.IsNullOrEmpty(locale.Locale)) 
        {
            entryTitle = $"{entryTitle}_{locale.Locale}";
        }
        
        var file = await fileManagementClient.UploadAsync(new MemoryStream(html), MediaTypeNames.Text.Html,
            $"{entryTitle}.html");

        return new(file);
    }

    [Action("Update entry content from HTML", Description = "Update content of a specific entry from HTML file")]
    public async Task<UpdateEntryFromHtmlResponse> UpdateEntryFromHtml(
        [ActionParameter] EntryOptionalRequest input,
        [ActionParameter] FileRequest fileRequest,
        [ActionParameter] LocaleRequest locale)
    {
        var file = await fileManagementClient.DownloadAsync(fileRequest.File);
        var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var (extractedContentTypeId, extractedEntryId) = HtmlToJsonConverter.ExtractContentTypeAndEntryId(memoryStream);

        var contentTypeId = input.ContentTypeId ?? extractedContentTypeId ??
            throw new("Content type ID is missing. Please provide it as an input or in the HTML file meta tag");
        var entryId = input.EntryId ?? extractedEntryId ??
            throw new("Entry ID is missing. Please provide it as an input or in the HTML file meta tag");

        var entry = await GetEntryJObject(contentTypeId, entryId);
        HtmlToJsonConverter.UpdateEntryFromHtml(memoryStream, entry, InvocationContext.Logger);

        await UpdateEntry(contentTypeId, entryId, entry, locale.Locale);

        return new()
        {
            ContentTypeId = contentTypeId,
            EntryId = entryId
        };
    }

    [Action("Get IDs from HTML", Description = "Extract content type and entry IDs from HTML file")]
    public GetIdsFromHtmlResponse ExtractContentTypeAndEntryId(
        [ActionParameter] FileRequest fileRequest)
    {
        var file = fileManagementClient.DownloadAsync(fileRequest.File).Result;
        var memoryStream = new MemoryStream();
        file.CopyTo(memoryStream);
        memoryStream.Position = 0;

        var (contentTypeId, entryId) = HtmlToJsonConverter.ExtractContentTypeAndEntryId(memoryStream);
        return new()
        {
            ContentTypeId = contentTypeId,
            EntryId = entryId
        };
    }

    #endregion

    #region Utils

    private List<string> ExtractAssetIdsFromJObject(JObject entryJObject, string? fileExtension)
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

            throw new(
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

    private void RemovePropertyByName(JToken token, string propertyName)
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