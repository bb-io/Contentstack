using Apps.Contentstack.DataSourceHandlers.Entry.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Entry;

public class EntryNumberDataHandler : EntryDataHandler
{
    public EntryNumberDataHandler(InvocationContext invocationContext, [ActionParameter] EntryNumberPropRequest request)
        : base(invocationContext, request.ContentTypeId)
    {
    }
}