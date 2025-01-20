using Apps.Contentstack.DataSourceHandlers.Entry.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Entry;

public class EntryBooleanDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] EntryBooleanPropRequest request)
    : EntryDataHandler(invocationContext, request.ContentTypeId);