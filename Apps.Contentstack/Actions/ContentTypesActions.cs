using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Response.ContentType;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.Contentstack.Actions;

[ActionList]
public class ContentTypesActions : AppInvocable
{
    public ContentTypesActions(InvocationContext invocationContext) : base(invocationContext)
    {
    }
    
    [Action("List content types", Description = "List all content types")]
    public async Task<ListContentTypesResponse> ListContentTypes([ActionParameter] ContentTypesRequest contentType)
    {
        var endpoint = "v3/content_types";
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);

        var response = await Client.ExecuteWithErrorHandling<ListContentTypesResponse>(request);

        if (contentType?.ContentTypeId != null && contentType.ContentTypeId.Any())
        {
            response.Items = response.Items
                .Where(x => contentType.ContentTypeId.Contains(x.Uid))
                .ToList();
        }

        return response;
    }
}