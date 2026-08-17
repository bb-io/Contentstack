using System.Text;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.HtmlConversion.Constants;
using Apps.Contentstack.Models.Entities;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Tests.Contentstack;

[TestClass]
public class NestedGroupRoundtripTests
{
    private const string EntryId = "bltef9de5a7d6ab4c8f";

    #region Fixtures

    private static JArray Schema => JArray.Parse(
        """
        [
          { "uid": "title", "data_type": "text", "multiple": false, "non_localizable": false },
          { "uid": "security", "data_type": "group", "multiple": false, "non_localizable": false,
            "schema": [
              { "uid": "eyebrow", "data_type": "text" },
              { "uid": "card_1", "data_type": "group",
                "schema": [
                  { "uid": "title", "data_type": "text" },
                  { "uid": "description", "data_type": "text" },
                  { "uid": "label", "data_type": "text" }
                ]
              }
            ]
          },
          { "uid": "three_ways", "data_type": "group", "multiple": false, "non_localizable": false,
            "schema": [
              { "uid": "card_1", "data_type": "group",
                "schema": [
                  { "uid": "title", "data_type": "text" },
                  { "uid": "use_cases", "data_type": "text", "multiple": true }
                ]
              }
            ]
          },
          { "uid": "build_on_remote", "data_type": "group", "multiple": false, "non_localizable": false,
            "schema": [
              { "uid": "card_api", "data_type": "group",
                "schema": [
                  { "uid": "cta", "data_type": "group",
                    "schema": [ { "uid": "label", "data_type": "text" } ]
                  }
                ]
              }
            ]
          },
          { "uid": "address", "data_type": "global_field", "multiple": false, "non_localizable": false,
            "schema": [
              { "uid": "lines", "data_type": "text", "multiple": true,
                "field_metadata": { "allow_rich_text": true } }
            ]
          }
        ]
        """);

    private static JObject SourceEntry() => JObject.Parse(
        """
        {
          "title": "EN title",
          "security": {
            "eyebrow": "EN eyebrow",
            "card_1": { "title": "EN card title", "description": "EN card description", "label": "EN card label" }
          },
          "three_ways": {
            "card_1": { "title": "EN ways title", "use_cases": [ "EN use case one", "EN use case two" ] }
          },
          "build_on_remote": {
            "card_api": { "cta": { "label": "EN cta label" } }
          },
          "address": {
            "lines": [ "<p>EN line one</p>", "<p>EN line two</p>" ]
          }
        }
        """);

    private static string Download(JObject entry) => Encoding.UTF8.GetString(
        JsonToHtmlConverter.ToHtml(entry, new ContentTypeBlockEntity { Schema = Schema }, null,
            "nested_group_repo", EntryId, "stackapikey", null));

    private static EntryImportReport Upload(string html, JObject entry)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
        return HtmlToJsonConverter.UpdateEntryFromHtml(stream, entry, null);
    }

    private static string TranslateViaXliff(string html)
    {
        var transformation = Transformation.Parse(html, "entry.html");
        transformation.SourceLanguage = "en";
        transformation.TargetLanguage = "es";

        foreach (var segment in transformation.GetUnits().SelectMany(x => x.Segments))
            segment.SetTarget(segment.GetSource().Replace("EN ", "ES "));

        return Transformation.Parse(transformation.Serialize(), "entry.xliff").Target().Serialize()!;
    }

    private static string Translate(string html) => html.Replace("EN ", "ES ");

    private static List<HtmlNode> LabelledNodes(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.Descendants()
            .Where(x => x.Attributes[ConversionConstants.PathAttr] is not null)
            .ToList();
    }

    #endregion

    #region Download — the invariants the upload step depends on

    [TestMethod]
    public void Download_NestedGroupContainers_CarryTheAbsoluteJsonPath()
    {
        var paths = LabelledNodes(Download(SourceEntry()))
            .Select(x => x.Attributes[ConversionConstants.PathAttr].Value)
            .ToList();

        CollectionAssert.Contains(paths, "security.card_1", "a sub-group must be labelled with its full path, not its uid");
        CollectionAssert.Contains(paths, "build_on_remote.card_api");
        CollectionAssert.Contains(paths, "build_on_remote.card_api.cta", "three groups deep is still a full path");
        CollectionAssert.Contains(paths, "three_ways.card_1.use_cases[0]", "a repeatable field inside a group is labelled per item");
        CollectionAssert.Contains(paths, "address.lines", "a repeatable field inside a global field needs its container path, which the upload resolves");
        CollectionAssert.DoesNotContain(paths, "card_1");
        CollectionAssert.DoesNotContain(paths, "cta");
        CollectionAssert.DoesNotContain(paths, "lines");
    }

    [TestMethod]
    public void Download_EveryBlackbirdKeyIsUnique()
    {
        var keys = LabelledNodes(Download(SourceEntry()))
            .Select(x => x.Attributes[ConversionConstants.BlackbirdKey]?.Value)
            .Where(x => x is not null)
            .ToList();

        var duplicates = keys.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

        Assert.AreEqual(0, duplicates.Count,
            "data-blackbird-key identifies a content unit and must be unique: " + string.Join(", ", duplicates));
    }

    [TestMethod]
    public void Download_EveryPathResolvesAgainstTheEntry()
    {
        var entry = SourceEntry();

        var unresolved = LabelledNodes(Download(entry))
            .Select(x => x.Attributes[ConversionConstants.PathAttr].Value)
            .Where(x => entry.SelectToken(x) is null)
            .ToList();

        Assert.AreEqual(0, unresolved.Count,
            "every labelled element must point at something the upload step can find: " + string.Join(", ", unresolved));
    }

    #endregion

    #region Upload — a target locale that already has every key

    [TestMethod]
    public void Upload_IntoCompleteTargetEntry_WritesEveryDepth()
    {
        var target = SourceEntry();
        var report = Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));
        Assert.AreEqual(0, report.CreatedFields.Count, "nothing was missing, so nothing should have been created");
        Assert.AreEqual("ES card title", target.SelectToken("security.card_1.title")!.Value<string>());
        Assert.AreEqual("ES cta label", target.SelectToken("build_on_remote.card_api.cta.label")!.Value<string>());
        Assert.AreEqual("ES use case two", target.SelectToken("three_ways.card_1.use_cases[1]")!.Value<string>());
    }

    [TestMethod]
    public void Upload_RepeatableFieldInsideGlobalField_IsWritten()
    {
        var target = SourceEntry();
        var report = Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual("<p>ES line one</p>", target.SelectToken("address.lines[0]")!.Value<string>());
        Assert.AreEqual("<p>ES line two</p>", target.SelectToken("address.lines[1]")!.Value<string>());
        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));
    }

    [TestMethod]
    public void Upload_AfterXliffRoundtrip_WritesEveryDepth()
    {
        var target = SourceEntry();
        var report = Upload(TranslateViaXliff(Download(SourceEntry())), target);

        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));
        Assert.AreEqual("ES card title", target.SelectToken("security.card_1.title")!.Value<string>());
        Assert.AreEqual("ES ways title", target.SelectToken("three_ways.card_1.title")!.Value<string>());
        Assert.AreEqual("ES cta label", target.SelectToken("build_on_remote.card_api.cta.label")!.Value<string>());
    }

    #endregion

    #region Upload — the regression: a drifted target locale

    [TestMethod]
    public void Upload_IntoLocaleMissingASubFieldKey_CreatesItAndReportsIt()
    {
        var target = SourceEntry();
        ((JObject)target.SelectToken("security.card_1")!).Remove("title");
        Assert.IsNull(target.SelectToken("security.card_1.title"), "arrange: the key must be absent");

        var report = Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual("ES card title", target.SelectToken("security.card_1.title")!.Value<string>(),
            "a key missing from the target locale must be created, not silently skipped");
        CollectionAssert.Contains(report.CreatedFields, "security.card_1.title");
        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));
    }

    [TestMethod]
    public void Upload_IntoLocaleMissingAWholeSubGroup_CreatesTheChain()
    {
        var target = SourceEntry();
        ((JObject)target["security"]!).Remove("card_1");
        ((JObject)target["build_on_remote"]!).Remove("card_api");

        var report = Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual("ES card title", target.SelectToken("security.card_1.title")!.Value<string>());
        Assert.AreEqual("ES card description", target.SelectToken("security.card_1.description")!.Value<string>());
        Assert.AreEqual("ES cta label", target.SelectToken("build_on_remote.card_api.cta.label")!.Value<string>(),
            "a three-level chain must be created intact");
        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));
    }

    [TestMethod]
    public void Upload_IntoLocaleMissingATopLevelGroup_CreatesIt()
    {
        var target = SourceEntry();
        target.Remove("three_ways");

        Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual("ES ways title", target.SelectToken("three_ways.card_1.title")!.Value<string>());
        Assert.AreEqual("ES use case one", target.SelectToken("three_ways.card_1.use_cases[0]")!.Value<string>(),
            "a list inside a created group must be rebuilt in order");
        Assert.AreEqual("ES use case two", target.SelectToken("three_ways.card_1.use_cases[1]")!.Value<string>());
    }

    [TestMethod]
    public void Upload_AfterXliffRoundtripIntoDriftedLocale_StillWritesTheMissingKey()
    {
        var target = SourceEntry();
        ((JObject)target.SelectToken("security.card_1")!).Remove("title");

        var report = Upload(TranslateViaXliff(Download(SourceEntry())), target);

        Assert.AreEqual("ES card title", target.SelectToken("security.card_1.title")!.Value<string>());
        CollectionAssert.Contains(report.CreatedFields, "security.card_1.title");
    }

    #endregion

    #region Upload — what must not happen

    [TestMethod]
    public void Upload_DoesNotInventValuesForGroupContainers()
    {
        var target = SourceEntry();

        Upload(Translate(Download(SourceEntry())), target);

        Assert.AreEqual(JTokenType.Object, target.SelectToken("security.card_1")!.Type,
            "a group container must stay a group, not become its own concatenated text");
        Assert.IsNull(target["card_1"], "a container must never be written at the entry root");
        Assert.IsNull(target["cta"]);
    }

    [TestMethod]
    public void Upload_EmptyValueForAMissingKey_CreatesNothing()
    {
        var target = SourceEntry();
        ((JObject)target.SelectToken("security.card_1")!).Remove("title");

        var html = Download(SourceEntry())
            .Replace(">EN card title<", "><");

        var report = Upload(html, target);

        Assert.IsNull(target.SelectToken("security.card_1.title"),
            "an empty value carries no translation, so it must not create the key");
        CollectionAssert.DoesNotContain(report.CreatedFields, "security.card_1.title");
    }

    [TestMethod]
    public void Upload_PathThatCannotBeCreated_IsReported()
    {
        var target = SourceEntry();
        target["security"] = new JValue("not a group at all");

        var report = Upload(Translate(Download(SourceEntry())), target);

        Assert.IsTrue(report.Errors.Any(x => x.Contains("security.card_1.title")),
            "an impossible path must be reported, not swallowed: " + string.Join(" | ", report.Errors));
    }

    #endregion

    #region The client's file

    [TestMethod]
    public void ClientExport_HasUniqueKeysAndResolvablePaths()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Input",
            "Remote MCP - Build on Remote — Home_en-us.html");

        if (!File.Exists(path))
            Assert.Inconclusive($"Client fixture not found at {path}");

        var keys = LabelledNodes(File.ReadAllText(path, Encoding.UTF8))
            .Select(x => x.Attributes[ConversionConstants.BlackbirdKey]?.Value)
            .Where(x => x is not null)
            .ToList();

        var duplicates = keys.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

        Assert.Inconclusive(duplicates.Count == 0
            ? "Fixture predates the fix but carries no duplicate keys — re-export it to make this a real assertion."
            : $"Fixture was exported by the old code and still carries {duplicates.Count} duplicate keys ({string.Join(", ", duplicates)}). Re-export the entry to refresh it.");
    }

    #endregion
}
