using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryNumberPropDataHandler : EntryPropDataHandler
{
    protected override string DataType => "number";


    public EntryNumberPropDataHandler(InvocationContext invocationContext,
        [ActionParameter] EntryNumberPropRequest request) : base(invocationContext, request.EntryId, request.ContentTypeId)
    {
    }
}