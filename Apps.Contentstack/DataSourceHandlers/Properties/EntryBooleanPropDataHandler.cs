using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryBooleanPropDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] EntryBooleanPropRequest request)
    : EntryPropDataHandler(invocationContext, request.EntryId,
        request.ContentTypeId)
{
    protected override string DataType => "boolean";
}