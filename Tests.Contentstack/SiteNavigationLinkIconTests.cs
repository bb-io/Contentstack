using System.Text;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Models.Entities;
using Newtonsoft.Json.Linq;

namespace Tests.Contentstack;

[TestClass]
public class SiteNavigationLinkIconTests
{
    private const string EntryId = "bltbf8dcf3a7012eef3";

    private static string Input(params string[] path)
        => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "..", "..", "..", "Input", .. path]),
            Encoding.UTF8);

    private static JObject Entry(string file)
        => (JObject)JObject.Parse(Input("site_navigation", file))["entry"]!.DeepClone();

    private static JObject Source => Entry("entry_rebrand_en-us.json");

    private static JArray Schema
        => (JArray)JObject.Parse(Input("site_navigation", "content_type_rebrand.json"))["content_type"]!["schema"]!;

    private static JObject Upload(JObject localized, params string[] syncExcludedFieldIds)
    {
        var source = Source;
        var html = JsonToHtmlConverter.ToHtml(source, new ContentTypeBlockEntity { Schema = Schema }, null,
            "site_navigation", EntryId, "stackapikey", null, ["icon"], syncExcludedFieldIds: syncExcludedFieldIds);

        SourceEntrySync.Apply(localized, source, Schema, new HashSet<string>(syncExcludedFieldIds));
        var before = (JObject)localized.DeepClone();

        using var stream = new MemoryStream(html);
        var report = HtmlToJsonConverter.UpdateEntryFromHtml(stream, localized, null, structureFromSource: true);

        Assert.AreEqual(0, report.Errors.Count, string.Join(Environment.NewLine, report.Errors));
        Assert.AreEqual(0, EntryPayloadValidator.Validate(before, localized).Count);
        return localized;
    }

    private static Dictionary<string, JToken?> LinkIcons(JObject entry)
        => entry.SelectTokens("main_navigation[*].*.main_groups[*].group.links[*]")
            .Concat(entry.SelectTokens("main_navigation[*].*.secondary_groups[*].group.links[*]"))
            .ToDictionary(x => x["_metadata"]!["uid"]!.ToString(), x => x.SelectToken("icon.icon"));

    private static int WithIcon(JObject entry) => LinkIcons(entry).Values.Count(x => x is JObject);

    [TestMethod]
    public void Fixtures_MatchTheReportedSituation()
    {
        Assert.AreEqual(39, WithIcon(Source));
        Assert.AreEqual(9, WithIcon(Entry("entry_rebrand_en-gb_v138.json")),
            "en-gb after the reported flight kept only 9 of the source's 39 link icons.");
    }

    [TestMethod]
    public void UploadEntry_LinkIconsNotInFile_AreTakenFromSourceWhenLocaleHasNone()
    {
        var result = Upload(Entry("entry_rebrand_en-gb_v137.json"));

        var sourceIcons = LinkIcons(Source);
        var icons = LinkIcons(result);
        var lost = sourceIcons
            .Where(x => x.Value is JObject && icons.GetValueOrDefault(x.Key) is not JObject)
            .Select(x => x.Key)
            .ToList();

        Assert.AreEqual(0, lost.Count, $"{lost.Count} link icons were cleared: {string.Join(", ", lost)}");
    }

    [TestMethod]
    public void UploadEntry_LinkIconSetInLocale_IsKept()
    {
        var localized = Entry("entry_rebrand_en-gb_v137.json");
        Upload(localized);
        var uid = LinkIcons(localized).First(x => x.Value is JObject).Key;
        var link = (JObject)localized.SelectTokens("$..links[*]").First(x => x["_metadata"]?["uid"]?.ToString() == uid);
        link["icon"]!["icon"] = new JObject { ["text"] = "en-gb-only", ["size"] = "nova" };

        var result = Upload(localized);

        Assert.AreEqual("en-gb-only", LinkIcons(result)[uid]?["text"]?.ToString());
    }

    [TestMethod]
    public void UploadEntry_LinkIconExcludedFromSync_StaysAsInLocale()
    {
        var localized = Entry("entry_rebrand_en-gb_v137.json");

        var result = Upload(localized, "icon");

        Assert.IsTrue(WithIcon(result) < WithIcon(Source),
            "With 'icon' excluded from sync, links the locale has no icon for stay without one.");
    }
}
