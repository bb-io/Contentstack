using Apps.Contentstack.DataSourceHandlers.Properties.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Properties;

public class EntryBooleanPropDataHandler : EntryPropDataHandler
{
    protected override string DataType => "boolean";


    public EntryBooleanPropDataHandler(InvocationContext invocationContext,
        [ActionParameter] EntryBooleanPropRequest request) : base(invocationContext, request)
    {
    }
}