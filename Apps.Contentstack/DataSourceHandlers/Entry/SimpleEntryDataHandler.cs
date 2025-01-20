using Apps.Contentstack.DataSourceHandlers.Entry.Base;
using Apps.Contentstack.Models.Request.Entry;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.Contentstack.DataSourceHandlers.Entry;

public class SimpleEntryDataHandler(InvocationContext invocationContext, [ActionParameter] EntryRequest request)
    : EntryDataHandler(invocationContext, request.ContentTypeId);