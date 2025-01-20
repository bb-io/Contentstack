using Apps.Contentstack.DataSourceHandlers.Entry.Base;
using Apps.Contentstack.Models.Request.Property;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Entry;

public class EntryNumberDataHandler(
    InvocationContext invocationContext,
    [ActionParameter] EntryNumberPropRequest request)
    : EntryDataHandler(invocationContext, request.ContentTypeId);