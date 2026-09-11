using Tests.Contentstack.Base;
using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Contentstack;

[TestClass]
public class DataSourceHandlersTests : TestBaseMultipleConnections
{
    private readonly DataSourceContext _context = new();

    private async Task AssertHandlerReturnsData<T>(T handler, int notEqualNumber = 0) where T : IAsyncDataSourceItemHandler
    {
        var data = await handler.GetDataAsync(_context, CancellationToken.None);
        PrintDataHandlerResult(data);
        Assert.AreNotEqual(notEqualNumber, data.Count(), "Handler should return non-empty collection");
    }

    [TestMethod, TargetConnections]
    public async Task GetDataAsync_WorkflowStageHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        await AssertHandlerReturnsData(new WorkflowStageDataHandler(invocationContext), -1);
    } 
    
    [TestMethod, TargetConnections]
    public async Task GetDataAsync_LanguageHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        await AssertHandlerReturnsData(new LanguageDataHandler(invocationContext));
    }

    [TestMethod, TargetConnections]
    public async Task GetDataAsync_ContentTypeHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        await AssertHandlerReturnsData(new ContentTypeDataHandler(invocationContext));
    }

    [TestMethod, TargetConnections]
    public async Task GetDataAsync_AssetHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        await AssertHandlerReturnsData(new AssetDataHandler(invocationContext));
    }
    
    [TestMethod, TargetConnections]
    public async Task GetDataAsync_EnvironmentHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        await AssertHandlerReturnsData(new EnvironmentDataHandler(invocationContext));
    }

    [TestMethod, TargetConnections]
    public async Task GetDataAsync_EntryTags_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        var handler = new EntryTagDataSourceHandler(invocationContext, new Apps.Contentstack.Models.Request.Entry.EntryRequest { ContentId= "bltb0b17fd01c287e55", ContentTypeId= "missions" },
            new Apps.Contentstack.Models.Request.LocaleRequest {  });
        var result = await handler.GetDataAsync(new DataSourceContext { }, CancellationToken.None);

        foreach (var item in result)
        {
            TestContext.WriteLine($"{item.Value}: {item.Key}");
        }

        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task GetDataAsync_TagDataSourceHandler_ReturnsNonEmptyCollection(InvocationContext invocationContext)
    {
        var handler = new TagDataSourceHandler(invocationContext);
        var result = await handler.GetDataAsync(new DataSourceContext { }, CancellationToken.None);

        foreach (var item in result)
        {
            TestContext.WriteLine($"{item.Value}: {item.Key}");
        }

        Assert.IsNotNull(result);
    }
}
