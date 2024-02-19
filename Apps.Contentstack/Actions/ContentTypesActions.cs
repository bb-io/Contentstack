using Apps.Contentstack.Api;
using Apps.Contentstack.Invocables;
using Apps.Contentstack.Models.Response.ContentType;
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
    public Task<ListContentTypesResponse> ListContentTypes()
    {
        var endpoint = "v3/content_types";
        var request = new ContentstackRequest(endpoint, Method.Get, Creds);
      
        return Client.ExecuteWithErrorHandling<ListContentTypesResponse>(request);
    }
}