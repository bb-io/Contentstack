using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Response.Entry;
using Apps.Contentstack.Models.Response.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.Actions;

[ActionList]
public class EntriesActions : AppInvocable
{
    public EntriesActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("Get entry string property", Description = "Get data of a specific entry string property")]
    public async Task<StringPropertyResponse> GetEntryStringProp(
        [ActionParameter] EntryStringPropRequest input)
    {
        var response = await Client.ContentType(input.ContentTypeId).Entry(input.EntryId).FetchAsync();
        var entry = Client.ProcessResponse<EntryResponse>(response).Entry;

        return new()
        {
            Uid = input.Property,
            Value = entry[input.Property]!.ToString()
        };
    }
}