using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryDatePropDataHandler : EntryPropDataHandler
{
    protected override string DataType => "isodate";


    public EntryDatePropDataHandler(InvocationContext invocationContext,
        [ActionParameter] EntryDatePropRequest request) : base(invocationContext, request.EntryId, request.ContentTypeId)
    {
    }
}