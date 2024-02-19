using System.Net.Mime;
using Apps.Contentstack.Api;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Property;
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
        [ActionParameter] SearchEntriesRequest input)
    {
        var endpoint = $"v3/content_types/{contentType.ContentTypeId}/entries"
            .SetQueryParameter("include_workflow", "true");

        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
        var result = await Client.ExecuteWithErrorHandling<ListEntriesResponse>(request);

        return new()
        {
            Entries = result.Entries
                .Where(x => input.WorkflowStage is null || x.Workflow?.Name == input.WorkflowStage)
        };
    }

    [Action("Get entry", Description = "Get details of a specific entry")]
    public async Task<EntryEntity> GetEntry(
        [ActionParameter] EntryRequest input)
    {
        var endpoint = $"v3/content_types/{input.ContentTypeId}/entries/{input.EntryId}";
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
      
        var response = await Client.ExecuteWithErrorHandling<EntryResponse>(request);

        return response.Entry;
    }

    #region Get property

    [Action("Get entry string property", Description = "Get data of a specific entry string property")]
    public async Task<StringPropertyResponse> GetEntryStringProp(
        [ActionParameter] EntryStringPropRequest input)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property).ToString()
        };
    }

    [Action("Get entry number property", Description = "Get data of a specific entry number property")]
    public async Task<NumberPropertyResponse> GetEntryNumberProp(
        [ActionParameter] EntryNumberPropRequest input)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property)
                .ToObject<decimal>()
        };
    }

    [Action("Get entry date property", Description = "Get data of a specific entry date property")]
    public async Task<DatePropertyResponse> GetEntryDateProp(
        [ActionParameter] EntryDatePropRequest input)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

        return new()
        {
            Uid = input.Property,
            Value = entry.Descendants().First(x => x.Parent is JProperty prop && prop.Name == input.Property)
                .ToObject<DateTime>()
        };
    }

    [Action("Get entry boolean property", Description = "Get data of a specific entry boolean property")]
    public async Task<BooleanPropertyResponse> GetEntryBooleanProp(
        [ActionParameter] EntryBooleanPropRequest input)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

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
        [ActionParameter] [Display("Value")] string value)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value);

    [Action("Set entry number property", Description = "Set data of a specific entry number property")]
    public Task SetEntryNumberProp(
        [ActionParameter] EntryNumberPropRequest input,
        [ActionParameter] [Display("Value")] decimal value)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value);

    [Action("Set entry date property", Description = "Set data of a specific entry date property")]
    public Task SetEntryDateProp(
        [ActionParameter] EntryDatePropRequest input,
        [ActionParameter] [Display("Value")] DateTime value)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value);

    [Action("Set entry boolean property", Description = "Set data of a specific entry boolean property")]
    public Task SetEntryBooleanProp(
        [ActionParameter] EntryBooleanPropRequest input,
        [ActionParameter] [Display("Value")] bool value)
        => SetEntryProperty(input.ContentTypeId, input.EntryId, input.Property, value);

    #endregion

    #region HTML conversion

    [Action("Get entry content as HTML", Description = "Get content of a specific entry as HTML file")]
    public async Task<FileResponse> GetEntryAsHtml(
        [ActionParameter] EntryRequest input)
    {
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);
        var html = JsonToHtmlConverter.ToHtml(entry);

        var file = await _fileManagementClient.UploadAsync(new MemoryStream(html), MediaTypeNames.Text.Html,
            $"{input.EntryId}.html");

        return new(file);
    }

    [Action("Update entry content from HTML", Description = "Update content of a specific entry from HTML file")]
    public async Task UpdateEntryFromHtml(
        [ActionParameter] EntryRequest input,
        [ActionParameter] FileRequest fileRequest)
    {
        var file = await _fileManagementClient.DownloadAsync(fileRequest.File);
        var entry = await GetEntryJObject(input.ContentTypeId, input.EntryId);

        var html = HtmlToJsonConverter.ToJson(file);
        html.Children().ToList().ForEach(x =>
        {
            var property = x as JProperty;
            entry[property!.Name] = property.Value;
        });
        
        await UpdateEntry(input.ContentTypeId, input.EntryId, entry);
    }

    #endregion

    #region Utils

    private async Task SetEntryProperty<T>(string contentTypeId, string entryId, string property, T value)
    {
        var entryObject = await GetEntryJObject(contentTypeId, entryId);

        var propertyValue = entryObject.Descendants()
            .First(x => x.Parent is JProperty prop && prop.Name == property) as JValue;
        propertyValue!.Value = value;

        await UpdateEntry(contentTypeId, entryId, entryObject);
    }

    private async Task UpdateEntry(string contentTypeId, string entryId, JObject entryObject)
    {
        var contentTypeObj = await GetContentType(contentTypeId);

        var fileProps = contentTypeObj.Schema
            .Descendants()
            .Where(x => x is JObject && x["data_type"]?.ToString() == "file")
            .Select(x => x["uid"]!.ToString())
            .ToList();

        fileProps.ForEach(x => RemovePropertyByName(entryObject, x));

        var request = new ContentstackRequest($"v3/content_types/{contentTypeId}/entries/{entryId}", Method.Put, Creds)
            .WithJsonBody(new
            {
                entry = entryObject
            });

        await Client.ExecuteWithErrorHandling(request);
    }

    private async Task<JObject> GetEntryJObject(string contentTypeId, string entryId)
    {
        var request = new ContentstackRequest($"v3/content_types/{contentTypeId}/entries/{entryId}", Method.Get, Creds);
        var response = await Client.ExecuteWithErrorHandling<EntryGenericResponse>(request);

        return response.Entry;
    }

    private async Task<ContentTypeContentEntity> GetContentType(string contentTypeId)
    {
        var request = new ContentstackRequest($"v3/content_types/{contentTypeId}", Method.Get, Creds);
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