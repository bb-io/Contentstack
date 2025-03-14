using Apps.Contentstack.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using OpenAITests.Base;

namespace Tests.Contentstack;

[TestClass]
public class DataSourceHandlersTests : TestBase
{
    private readonly DataSourceContext _context;

    public DataSourceHandlersTests()
    {
        _context = new DataSourceContext();
    }

    private async Task AssertHandlerReturnsData<T>(T handler, int notEqualNumber = 0) where T : IAsyncDataSourceItemHandler
    {
        var data = await handler.GetDataAsync(_context, CancellationToken.None);
        
        foreach (var item in data)
        {
            Console.WriteLine($"{item.Value}: {item.DisplayName}");
        }

        Assert.AreNotEqual(notEqualNumber, data.Count(), "Handler should return non-empty collection");
    }

    [TestMethod]
    public async Task GetDataAsync_WorkflowStageHandler_ReturnsNonEmptyCollection()
    {
        await AssertHandlerReturnsData(new WorkflowStageDataHandler(InvocationContext), -1);
    }

    [TestMethod]
    public async Task GetDataAsync_LanguageHandler_ReturnsNonEmptyCollection()
    {
        await AssertHandlerReturnsData(new LanguageDataHandler(InvocationContext));
    }

    [TestMethod]
    public async Task GetDataAsync_ContentTypeHandler_ReturnsNonEmptyCollection()
    {
        await AssertHandlerReturnsData(new ContentTypeDataHandler(InvocationContext));
    }

    [TestMethod]
    public async Task GetDataAsync_AssetHandler_ReturnsNonEmptyCollection()
    {
        await AssertHandlerReturnsData(new AssetDataHandler(InvocationContext));
    }
    
    [TestMethod]
    public async Task GetDataAsync_EnvironmentHandler_ReturnsNonEmptyCollection()
    {
        await AssertHandlerReturnsData(new EnvironmentDataHandler(InvocationContext));
    }
}
