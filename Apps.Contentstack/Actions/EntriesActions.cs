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
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.Sdk.Utils.Extensions.Http;
using Blackbird.Applications.Sdk.Utils.Extensions.String;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Contentstack.Actions;

[ActionList]
public class EntriesActions : AppInvocable
{
    private readonly IFileManagementClient _fileManagementClient;

    public EntriesActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : base(
        invocationContext)
    {
        _fileManagementClient = fileManagementClient;
    }

    [Action("Search entries", Description = "Search for entries based on the provided filters")]
    public async Task<ListEntriesResponse> SearchEntries(
        [ActionParameter] ContentTypeRequest contentType,
        [ActionParameter] WorkflowStageFilterRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var endpoint = $"v3/content_types/{contentType.ContentTypeId}/entries"
            .SetQueryParameter("include_workflow", "true");

        if (!string.IsNullOrWhiteSpace(locale.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var result = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(request);

        var entries = result.Entries
            .Where(x => input.WorkflowStageId is null || x.Workflow?.Uid == input.WorkflowStageId)
            .ToArray();
        
        return new()
        {
            Entries = entries,
            Count = entries.Length
        };
    }

    [Action("Get entry", Description = "Get details of a specific entry")]
    public async Task<EntryEntity> GetEntry(
        [ActionParameter] EntryRequest input,
        [ActionParameter] LocaleRequest locale)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.EntryId}"
            .SetQueryParameter("include_workflow", "true");

        if (!string.IsNullOrWhiteSpace(locale.Locale))
            endpoint = endpoint.SetQueryParameter("locale", locale.Locale);

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<EntryResponse>(request);

        return response.Entry;
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
        [ActionParameter] EntryRequest input, [ActionParameter] LocaleRequest locale)
    {
        var contentType = await GetContentType(input.ContentTypeId);

        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId, locale.Locale);
        var html = JsonToHtmlConverter.ToHtml(entry, contentType, InvocationContext.Logger);

        var file = await _fileManagementClient.UploadAsync(new MemoryStream(html), MediaTypeNames.Text.Html,
            $"{input.EntryId}.html");

        return new(file);
    }

    [Action("Update entry content from HTML", Description = "Update content of a specific entry from HTML file")]
    public async Task UpdateEntryFromHtml(
        [ActionParameter] EntryRequest input,
        [ActionParameter] FileRequest fileRequest,
        [ActionParameter] LocaleRequest locale)
    {
        var file = await _fileManagementClient.DownloadAsync(fileRequest.File);
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

        HtmlToJsonConverter.UpdateEntryFromHtml(file, entry, InvocationContext.Logger);
        await UpdateEntry(input.ContentTypeId, input.EntryId, entry, locale.Locale);
    }

    #endregion

    #region Utils

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
            endpoint = endpoint.SetQueryParameter("locale", locale);

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