using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryStringPropDataHandler : EntryPropDataHandler
{
    protected override string DataType => "text";


    public EntryStringPropDataHandler(InvocationContext invocationContext,
        [ActionParameter] EntryStringPropRequest request) : base(invocationContext, request.EntryId, request.ContentTypeId)
    {
    }
}