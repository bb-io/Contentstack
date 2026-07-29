using Apps.Contentstack.Helper;
using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json.Linq;

namespace Tests.Contentstack;

[TestClass]
public class EntryReferenceExtractorTests
{
    private static ContentTypeBlockEntity BuildContentType() => new()
    {
        Uid = "page",
        Title = "Page",
        Schema = JArray.Parse("""
        [
          { "uid": "title", "data_type": "text" },
          { "uid": "related_pages", "data_type": "reference", "reference_to": ["page"], "multiple": true },
          { "uid": "author", "data_type": "reference", "reference_to": ["author"] },
          {
            "uid": "section",
            "data_type": "group",
            "schema": [ { "uid": "cta", "data_type": "reference", "reference_to": ["cta"] } ]
          }
        ]
        """)
    };

    private static JObject BuildEntry() => JObject.Parse("""
    {
      "uid": "blt_root",
      "title": "Root entry",
      "related_pages": [ { "uid": "blt_page1", "_content_type_uid": "page" } ],
      "author": [ { "uid": "blt_author1", "_content_type_uid": "author" } ],
      "section": { "cta": [ { "uid": "blt_cta1", "_content_type_uid": "cta" } ] }
    }
    """);

    [TestMethod]
    public void ExtractReferencedEntriesWithContentTypes_NoExclusions_ReturnsAllReferences()
    {
        var result = EntryReferenceExtractor.ExtractReferencedEntriesWithContentTypes(BuildEntry(), BuildContentType());

        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains(("blt_page1", "page")));
        Assert.IsTrue(result.Contains(("blt_author1", "author")));
        Assert.IsTrue(result.Contains(("blt_cta1", "cta")));
    }

    [TestMethod]
    public void ExtractReferencedEntriesWithContentTypes_ExcludedContentType_SkipsThoseReferences()
    {
        var result = EntryReferenceExtractor.ExtractReferencedEntriesWithContentTypes(
            BuildEntry(), BuildContentType(), ["author"]);

        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result.Any(x => x.ContentTypeUid == "author"));
    }

    [TestMethod]
    public void ExtractReferencedEntriesWithContentTypes_ExcludedContentTypeDifferentCasing_SkipsThoseReferences()
    {
        var result = EntryReferenceExtractor.ExtractReferencedEntriesWithContentTypes(
            BuildEntry(), BuildContentType(), ["AUTHOR", "CTA"]);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.Contains(("blt_page1", "page")));
    }

    [TestMethod]
    public void ExtractReferencedEntriesWithContentTypes_AllContentTypesExcluded_ReturnsEmpty()
    {
        var result = EntryReferenceExtractor.ExtractReferencedEntriesWithContentTypes(
            BuildEntry(), BuildContentType(), ["page", "author", "cta"]);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void ExtractReferencedEntryUids_ReturnsAllReferencedUids()
    {
        var result = EntryReferenceExtractor.ExtractReferencedEntryUids(BuildEntry(), BuildContentType());

        Assert.AreEqual(3, result.Count);
        CollectionAssert.Contains(result, "blt_author1");
    }
}
