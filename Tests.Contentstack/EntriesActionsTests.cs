using Apps.Contentstack.Actions;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Workflow;
using Blackbird.Applications.Sdk.Common.Files;
using OpenAITests.Base;

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
            ContentTypeId = "missions",
            EntryId = "bltb0b17fd01c287e55"
        };
        var localeRequest = new LocaleRequest { };
        var fileRequest = new FileExtensionRequest { };

        var result = await action.GetEntry(entryRequest, localeRequest, fileRequest);

        Console.WriteLine(result.Uid);
        Console.WriteLine(result.Locale);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetEntryAsHtml_WithValidLocale_ReturnsHtmlContent()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "repeating_fields",
            EntryId = "bltda1e0b08782659f5"
        };
        var localeRequest = new LocaleRequest
        {
            Locale = "en-us"
        };

        var result = await action.GetEntryAsHtml(entryRequest, localeRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
    }

    
    [TestMethod]
    public async Task GetEntryAsHtml_ComplexObject_ReturnsHtmlContent()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "repeating_complex_fields",
            EntryId = "bltca8323a6a8c3c3c1"
        };
        var localeRequest = new LocaleRequest
        {
            Locale = "en-us"
        };

        var result = await action.GetEntryAsHtml(entryRequest, localeRequest);

        Assert.IsNotNull(result);
        Assert.IsNotNull(result.File);
    }

    [TestMethod]
    public async Task UpdateEntryFromHtml_WithAttachedFile_ShouldUpdateEntry()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var fileReference = new FileReference { Name = "Repeitable onderzoek_en-us.html" };
        var fileRequest = new FileRequest { File = fileReference };

        // Act
        var result = await action.UpdateEntryFromHtml(new(), fileRequest, new());

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("repeating_fields", result.ContentTypeId);
    }

    
    [TestMethod]
    public async Task UpdateEntryFromHtml_ComplexEntry_ShouldUpdateEntry()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var fileReference = new FileReference { Name = "Complex entry_en_us.html" };
        var fileRequest = new FileRequest { File = fileReference };

        // Act
        var result = await action.UpdateEntryFromHtml(new(), fileRequest, new());

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("repeating_complex_fields", result.ContentTypeId);
    }

    [TestMethod]
    public async Task AddTagToEntry_WithValidTagAndEntry_ReturnsUpdatedEntry()
    {
        var action = new EntriesActions(InvocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            EntryId = "bltb0b17fd01c287e55"
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
            EntryId = "bltb0b17fd01c287e55"
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
        var contenteRequest = new ContentTypeRequest { ContentTypeId = "missions", };
        var localeRequest = new LocaleRequest { };
        var workflowRequest = new WorkflowStageFilterRequest { };
        var tagFilter = new TagFilterRequest { Tag = "insights explore toolkit1" };

        var result = await action.SearchEntries(contenteRequest, workflowRequest, localeRequest, tagFilter);
        foreach (var item in result.Entries)
        {
            Console.WriteLine($"{item.Uid} - {item.Title} - {item.Tags}");
            Assert.IsNotNull(item);
        }
    }
}
