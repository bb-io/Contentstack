using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models;
using Apps.Contentstack.Models.Request.Property;
using Apps.Contentstack.Models.Response.ContentType;
using Apps.Contentstack.Models.Response.Entry;
using Apps.Contentstack.Models.Response.Property;
using Apps.Contentstack.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.Contentstack.Actions;

[ActionList]
public class EntriesActions : AppInvocable
{
    public EntriesActions(InvocationContext invocationContext) : base(invocationContext)
    {
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

    #region Utils

    private async Task SetEntryProperty<T>(string contentTypeId, string entryId, string property, T value)
    {
        var contentType = Client.ContentType(contentTypeId);
        var entry = contentType.Entry(entryId);
        
        var response = await entry.FetchAsync();
        var entryObject = Client.ProcessResponse<EntryResponse>(response).Entry;
     
        var propertyValue = entryObject.Descendants()
            .First(x => x.Parent is JProperty prop && prop.Name == property) as JValue;
        propertyValue!.Value = value;

        var contentTypeResponse = await contentType.FetchAsync();
        var contentTypeObj = Client.ProcessResponse<ContentTypeResponse>(contentTypeResponse).ContentType;

        var fileProps = contentTypeObj.Schema
            .Descendants()
            .Where(x => x is JObject && x["data_type"]?.ToString() == "file")
            .Select(x => x["uid"]!.ToString())
            .ToList();

        fileProps.ForEach(x => RemovePropertyByName(entryObject, x));

        await ContentstackErrorHandler.HandleRequest(() => entry.UpdateAsync(new EntryJObject(entryObject)));
    }

    private async Task<JObject> GetEntryJObject(string contentTypeId, string entryId)
    {
        var response = await ContentstackErrorHandler.HandleRequest(() =>
            Client.ContentType(contentTypeId).Entry(entryId).FetchAsync());

        return Client.ProcessResponse<EntryResponse>(response).Entry;
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

    #endregion
}