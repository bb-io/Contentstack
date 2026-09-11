using Tests.Contentstack.Base;
using Apps.Contentstack.Actions;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Workflow;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Contentstack;

[TestClass]
public class EntriesActionsTests : TestBaseMultipleConnections
{
    [TestMethod, TargetConnections]
    public async Task ReplaceEntryAssets_IsSuccess(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryInput = new EntryRequest
        {
            ContentTypeId = "test",
            ContentId = "blt13a2690c316a972c"
        };
        var replaceInput = new ReplaceEntryAssetsRequest
        {
            ReplaceAssetsContaining = "ukrainian",
            WithAssetsContaining = "english"
        };

        // Act
        await actions.ReplaceEntryAssets(entryInput, replaceInput, null);
    }

    [TestMethod, TargetConnections]
    public async Task GetEntry_WithValidEntryIdAndContentType_ReturnsEntryObject(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a"
        };

        // Act
        var result = await actions.GetEntry(entryRequest, new LocaleRequest(), new FileExtensionRequest());

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.AreEqual(entryRequest.ContentTypeId, result.ContentTypeId);
    }

    [TestMethod, TargetConnections]
    public async Task DownloadEntryContent_ReturnsHtmlContent(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new DownloadEntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a",
            IncludeReferencedEntryUids = true
        };

        // Act
        var result = await actions.GetEntryAsHtml(entryRequest, new LocaleRequest());

        // Assert
        TestContext.WriteLine(result.Content.Name);
        TestContext.WriteLine(string.Join(", ", result.ReferencedEntryUids ?? []));
        Assert.IsNotNull(result.Content);
    }

    [TestMethod, TargetConnections]
    public async Task UploadEntryContent_IsSuccess(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var request = new UploadEntryRequest { Content = new FileReference { Name = "test.html" } };

        // Act
        var result = await actions.UpdateEntryFromHtml(request);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task AddTagToEntry_WithValidTagAndEntry_ReturnsUpdatedEntry(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };

        // Act
        var result = await actions.AddTagToEntry(entryRequest, "insights explore toolkit2", new LocaleRequest());

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task RemoveTagFromEntry_WithValidTagAndEntry_ReturnsUpdatedEntry(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };

        // Act
        var result = await actions.RemoveTagFromEntry(entryRequest, "insights explore toolkit1", new LocaleRequest());

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task GetEntryLocales_WithValidEntry_ReturnsLocaleList(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "test-123",
            ContentId = "blt06567cfc9ee0a966"
        };

        // Act
        var result = await actions.GetEntryLocales(entryRequest);

        // Assert
        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Locales);
        Assert.IsTrue(result.Locales.Any());
    }

    [TestMethod, TargetConnections]
    public async Task SearchEntries_WithValidFilters_ReturnsMatchingEntries(InvocationContext invocationContext)
    {
        // Arrange
        var actions = new EntriesActions(invocationContext, FileManager);
        var searchRequest = new SearchEntriesRequest { ContentTypeIds = ["page"] };

        // Act
        var result = await actions.SearchEntries(
            searchRequest,
            new WorkflowStageFilterRequest(),
            new LocaleRequest(),
            new TagFilterRequest(),
            new UpdatedAtFilterRequest(),
            null);

        // Assert
        foreach (var item in result.Entries)
        {
            TestContext.WriteLine($"{item.ContentId} - {item.Title} - {item.Tags}");
            Assert.IsNotNull(item);
            Assert.AreEqual("page", item.ContentTypeId);
        }
    }
}