using System.Text;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Models.Entities;
using Blackbird.Filters.Transformations;
using Newtonsoft.Json.Linq;

namespace Tests.Contentstack;

[TestClass]
public class TranslationUnitKeyTests
{
    private const string EntryId = "blt4727d2b06b3b7402";

    #region Fixtures

    private static JArray Schema => JArray.Parse(
        """
        [
          { "uid": "title", "data_type": "text", "multiple": false, "non_localizable": false },
          { "uid": "accordion_items", "data_type": "blocks", "multiple": true, "non_localizable": false,
            "blocks": [
              { "uid": "accordion_item", "title": "Accordion Item",
                "schema": [
                  { "uid": "title", "data_type": "text", "multiple": false, "non_localizable": false },
                  { "uid": "description", "data_type": "json", "multiple": false, "non_localizable": false }
                ]
              }
            ]
          }
        ]
        """);

    private static JObject Item(string title, params string[] paragraphs)
    {
        var children = new JArray(paragraphs.Select(x => new JObject
        {
            ["type"] = "p",
            ["children"] = new JArray(new JObject { ["text"] = x })
        }));

        return new JObject
        {
            ["accordion_item"] = new JObject
            {
                ["title"] = title,
                ["description"] = new JObject { ["type"] = "doc", ["children"] = children }
            }
        };
    }

    private static JObject SourceEntry() => new()
    {
        ["title"] = "EN Research: International payroll processing",
        ["accordion_items"] = new JArray(
            Item("EN How do you pay workers in other countries?", "EN Pay one.", "EN Pay two.", "EN Pay three."),
            Item("EN How do you calculate global payroll?", "EN Calculate one.", " "),
            Item("EN How much should you pay your international employees?", "EN Amount one.", "EN Amount two."))
    };

    private static JObject DriftedEntry()
    {
        var entry = SourceEntry();
        entry.SelectToken("accordion_items[0].accordion_item.description.children[1].children[0].text")!
            .Replace(new JValue(" "));
        return entry;
    }

    private static string Download(JObject entry) => Encoding.UTF8.GetString(
        JsonToHtmlConverter.ToHtml(entry, new ContentTypeBlockEntity { Schema = Schema }, null,
            "section_accordion", EntryId, "stackapikey", null));

    private static EntryImportReport Upload(string html, JObject entry)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
        return HtmlToJsonConverter.UpdateEntryFromHtml(stream, entry, null);
    }

    private static Transformation Parse(string html)
    {
        var transformation = Transformation.Parse(html, "entry.html");
        transformation.SourceLanguage = "en-us";
        transformation.TargetLanguage = "en-au";
        return transformation;
    }

    #endregion

    [TestMethod]
    public void Download_EveryTranslationUnit_CarriesItsOwnKey()
    {
        var units = Parse(Download(SourceEntry())).GetUnits().ToList();

        var anonymous = units.Where(x => string.IsNullOrEmpty(x.Key)).Select(x => x.Name).ToList();

        Assert.AreEqual(0, anonymous.Count,
            $"{anonymous.Count} of {units.Count} units have no key, so the lake can only match them by position: "
            + string.Join(", ", anonymous));
    }

    [TestMethod]
    public void Download_UnitKeys_AreUnique()
    {
        var keys = Parse(Download(SourceEntry())).GetUnits()
            .Select(x => x.Key).Where(x => !string.IsNullOrEmpty(x)).ToList();

        var duplicates = keys.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

        Assert.AreEqual(0, duplicates.Count, "a key must identify one unit: " + string.Join(", ", duplicates));
    }

    [TestMethod]
    public void Download_WhenAParagraphGoesEmpty_SurvivingUnitsKeepTheirKeys()
    {
        var before = Parse(Download(SourceEntry())).GetUnits()
            .ToDictionary(x => x.Key!, x => x.Segments[0].GetSource());

        var after = Parse(Download(DriftedEntry())).GetUnits()
            .ToDictionary(x => x.Key!, x => x.Segments[0].GetSource());

        var moved = after
            .Where(x => before.ContainsKey(x.Key) && before[x.Key] != x.Value)
            .Select(x => $"{x.Key}: [{before[x.Key]}] -> [{x.Value}]")
            .ToList();

        Assert.AreEqual(0, moved.Count,
            "a key must keep pointing at the same content even when earlier units disappear: "
            + string.Join(" | ", moved));
    }

    [TestMethod]
    public void Upload_TranslationsMatchedByKeyAcrossADriftedExport_LandInTheirOwnFields()
    {
        var cache = Parse(Download(SourceEntry())).GetUnits()
            .ToDictionary(x => x.Key!, x => x.Segments[0].GetSource().Replace("EN ", "TR "));

        var drifted = Parse(Download(DriftedEntry()));

        foreach (var unit in drifted.GetUnits().Where(x => cache.ContainsKey(x.Key!)))
            unit.Segments[0].SetTarget(cache[unit.Key!]);

        var target = Transformation.Parse(drifted.Serialize(), "entry.xliff").Target().Serialize()!;

        var entry = DriftedEntry();
        Upload(target, entry);

        var titles = entry["accordion_items"]!
            .Select(x => x.SelectToken("accordion_item.title")!.Value<string>()!)
            .ToList();

        CollectionAssert.AreEqual(new[]
        {
            "TR How do you pay workers in other countries?",
            "TR How do you calculate global payroll?",
            "TR How much should you pay your international employees?"
        }, titles, "each title must receive its own translation: " + string.Join(" | ", titles));

        Assert.AreEqual("TR Amount two.",
            entry.SelectToken("accordion_items[2].accordion_item.description.children[1].children[0].text")!
                .Value<string>());
    }
}
