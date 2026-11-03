using Tests.Contentstack.Base;
using Apps.Contentstack.Actions;
using Apps.Contentstack.DataSourceHandlers;
using Apps.Contentstack.Models.Request;
using Apps.Contentstack.Models.Request.Entry;
using Apps.Contentstack.Models.Request.Workflow;
using Apps.Contentstack.Models.Response.Entry;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Contentstack;

[TestClass]
public class EntriesActionsTests : TestBaseMultipleConnections
{
    [TestMethod, TargetConnections]
    public async Task ReplaceEntryAssets_IsSuccess(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryInput = new EntryRequest
        {
            ContentTypeId = "repro_replace_assets",
            ContentId = "blt07dab3a7ceb2909e"
        };
        var replaceInput = new ReplaceEntryAssetsRequest
        {
            ReplaceAssetsContaining = "english",
            WithAssetsContaining = "japanese"
        };
        var locale = new LocaleRequest { Locale = "en-us" };

        await actions.ReplaceEntryAssets(entryInput, replaceInput, locale);
    }

    [TestMethod, TargetConnections]
    public async Task GetEntry_WithValidEntryIdAndContentType_ReturnsEntryObject(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a"
        };

        var result = await actions.GetEntry(entryRequest, new LocaleRequest(), new FileExtensionRequest());

        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.AreEqual(entryRequest.ContentTypeId, result.ContentTypeId);
    }

    [TestMethod, TargetConnections]
    public async Task DownloadEntryContent_ReturnsHtmlContent(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new DownloadEntryRequest
        {
            ContentTypeId = "page",
            ContentId = "blt3722af2e4979b90a",
            IncludeReferencedEntryUids = true
        };

        var result = await actions.GetEntryAsHtml(entryRequest, new LocaleRequest());

        TestContext.WriteLine(result.Content.Name);
        TestContext.WriteLine(string.Join(", ", result.ReferencedEntryUids ?? []));
        Assert.IsNotNull(result.Content);
    }

    [TestMethod, TargetConnections]
    public async Task UploadEntryContent_IsSuccess(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var request = new UploadEntryRequest { Content = new FileReference { Name = "test.html" } };

        var result = await actions.UpdateEntryFromHtml(request);

        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task AddTagToEntry_WithValidTagAndEntry_ReturnsUpdatedEntry(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };

        var result = await actions.AddTagToEntry(entryRequest, "insights explore toolkit2", new LocaleRequest());

        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task RemoveTagFromEntry_WithValidTagAndEntry_ReturnsUpdatedEntry(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "missions",
            ContentId = "bltb0b17fd01c287e55"
        };

        var result = await actions.RemoveTagFromEntry(entryRequest, "insights explore toolkit1", new LocaleRequest());

        PrintResult(result);
        Assert.IsNotNull(result);
    }

    [TestMethod, TargetConnections]
    public async Task GetEntryLocales_WithValidEntry_ReturnsLocaleList(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var entryRequest = new EntryRequest
        {
            ContentTypeId = "test-123",
            ContentId = "blt06567cfc9ee0a966"
        };

        var result = await actions.GetEntryLocales(entryRequest);

        PrintResult(result);
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Locales);
        Assert.IsTrue(result.Locales.Any());
    }

    [TestMethod, TargetConnections]
    public async Task SearchEntries_WithValidFilters_ReturnsMatchingEntries(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var searchRequest = new SearchEntriesRequest { ContentTypeIds = ["page"] };

        var result = await actions.SearchEntries(
            searchRequest,
            new WorkflowStageFilterRequest(),
            new LocaleRequest(),
            new TagFilterRequest(),
            new UpdatedAtFilterRequest(),
            null,
            new PublishedByFilterRequest());

        foreach (var item in result.Entries)
        {
            TestContext.WriteLine($"{item.ContentId} - {item.Title} - {item.Tags}");
            Assert.IsNotNull(item);
            Assert.AreEqual("page", item.ContentTypeId);
        }
    }

    [TestMethod, TargetConnections]
    public async Task SearchEntries_NotPublishedByUserInEnvironment_ExcludesEntriesThatUserPublished(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var environments = await new EnvironmentNameDataHandler(invocationContext)
            .GetDataAsync(new DataSourceContext(), CancellationToken.None);
        var contentTypes = await new ContentTypesActions(invocationContext).ListContentTypes(null);
        var selectableUserIds = (await new UserDataHandler(invocationContext)
            .GetDataAsync(new DataSourceContext(), CancellationToken.None)).Select(x => x.Value).ToList();

        foreach (var environment in environments.Select(x => x.Value))
        {
            foreach (var contentType in contentTypes.Items.Select(x => x.Uid))
            {
                var searchRequest = new SearchEntriesRequest { ContentTypeIds = [contentType], Environment = environment };
                var unfiltered = await SearchEntries(actions, searchRequest, new PublishedByFilterRequest());
                var publisher = unfiltered.Entries.SelectMany(x => x.PublishDetails).Select(x => x.User).FirstOrDefault();
                if (publisher is null)
                    continue;

                var filtered = await SearchEntries(actions, searchRequest,
                    new PublishedByFilterRequest { ExcludedPublisherUserId = publisher });

                TestContext.WriteLine($"Unfiltered: {unfiltered.Count}, filtered: {filtered.Count}");
                Assert.Contains(publisher, selectableUserIds);
                Assert.IsLessThan(unfiltered.Count, filtered.Count);
                Assert.IsFalse(filtered.Entries.Any(x => x.PublishDetails.Any(d => d.User == publisher && d.Locale == x.Locale)));
                return;
            }
        }

        Assert.Inconclusive("No published entries found");
    }

    [TestMethod, TargetConnections]
    public async Task SearchEntries_NotPublishedByUserWithoutEnvironment_ReadsPublishDetailsOfEveryEnvironment(InvocationContext invocationContext)
    {
        var actions = new EntriesActions(invocationContext, FileManager);
        var contentTypes = await new ContentTypesActions(invocationContext).ListContentTypes(null);

        foreach (var contentType in contentTypes.Items.Select(x => x.Uid))
        {
            var searchRequest = new SearchEntriesRequest { ContentTypeIds = [contentType] };
            var withPublishDetails = await SearchEntries(actions, searchRequest,
                new PublishedByFilterRequest { ExcludedPublisherUserId = "user-without-publications" });
            var publisher = withPublishDetails.Entries
                .SelectMany(x => x.PublishDetails.Where(d => d.Locale == x.Locale))
                .Select(x => x.User)
                .FirstOrDefault();
            if (publisher is null)
                continue;

            var filtered = await SearchEntries(actions, searchRequest,
                new PublishedByFilterRequest { ExcludedPublisherUserId = publisher });

            TestContext.WriteLine($"Unfiltered: {withPublishDetails.Count}, filtered: {filtered.Count}");
            Assert.IsLessThan(withPublishDetails.Count, filtered.Count);
            return;
        }

        Assert.Inconclusive("No published entries found");
    }

    private static Task<ListEntriesResponse> SearchEntries(EntriesActions actions, SearchEntriesRequest searchRequest,
        PublishedByFilterRequest publishedByFilter)
        => actions.SearchEntries(
            searchRequest,
            new WorkflowStageFilterRequest(),
            new LocaleRequest(),
            new TagFilterRequest(),
            new UpdatedAtFilterRequest(),
            new UpdatedByFilterRequest(),
            publishedByFilter);
}
