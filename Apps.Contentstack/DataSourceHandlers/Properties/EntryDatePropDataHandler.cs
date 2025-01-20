using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryDatePropDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] EntryDatePropRequest request)
    : EntryPropDataHandler(invocationContext, request.EntryId, request.ContentTypeId)
{
    protected override string DataType => "isodate";
}