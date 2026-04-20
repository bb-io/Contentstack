using Tests.Contentstack.Base;
using Apps.Contentstack.Actions;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Workflow;
using Blackbird.Applications.Sdk.Common.Files;

namespace Tests.Contentstack;

[TestClass]
public class EntriesActionsTests : TestBase
{
    [TestMethod]
    public async Task GetEntry_WithValidEntryIdAndContentType_ReturnsEntryObject()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a"
        };
        var localeRequest = new LocaleRequest { };
        var fileRequest = new FileExtensionRequest { };

        var result = await action.GetEntry(entryRequest, localeRequest, fileRequest);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        Console.WriteLine(json);

        Console.WriteLine(result.ContentId);
        Console.WriteLine(result.Locale);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task DownloadEntryContent_ReturnsHtmlContent()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a"
        };
        var localeRequest = new LocaleRequest
        {
            //Locale = "en"
        };

        var result = await action.GetEntryAsHtml(entryRequest, localeRequest);
        
        Console.WriteLine(result.Content.Name);
        Assert.IsNotNull(result.Content);
    }

    [TestMethod]
    public async Task UploadEntryContent_IsSuccess()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var fileReference = new FileReference { Name = "test.html" };
        var request = new UploadEntryRequest { Content = fileReference };

        // Act
        var result = await action.UpdateEntryFromHtml(request);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task AddTagToEntry_WithValidTagAndEntry_ReturnsUpdatedEntry()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };
        var localeRequest = new LocaleRequest { };
        var fileRequest = new FileExtensionRequest { };

        var result = await action.AddTagToEntry(entryRequest, "insights explore toolkit2", localeRequest);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task RemoveTagFromEntry_WithValidTagAndEntry_ReturnsUpdatedEntry()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };
        var localeRequest = new LocaleRequest { };
        var fileRequest = new FileExtensionRequest { };

        var result = await action.RemoveTagFromEntry(entryRequest, "insights explore toolkit1", localeRequest);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SearchEntries_WithValidFilters_ReturnsMatchingEntries()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var contenteRequest = new ContentTypeRequest { ContentTypeId = "page", };
        var localeRequest = new LocaleRequest { };
        var workflowRequest = new WorkflowStageFilterRequest { };
        var tagFilter = new TagFilterRequest { /*Tag = "insights explore toolkit1"*/ };

        var result = await action.SearchEntries(contenteRequest, workflowRequest, localeRequest, tagFilter);
        foreach (var item in result.Entries)
        {
            Console.WriteLine($"{item.ContentId} - {item.Title} - {item.Tags}");
            Assert.IsNotNull(item);
        }
    }
}
