using System.Text;
using Apps.Contentstack.HtmlConversion;
using Apps.Contentstack.Models.Entities;
using Blackbird.Filters.Transformations;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Tests.Contentstack;

[TestClass]
public class InlineEmbeddedEntryTests
{
    private const string EntryId = "bltentry1";

    private static JArray Schema => JArray.Parse(
        """
        [
          { "uid": "title", "data_type": "text", "multiple": false, "non_localizable": false },
          { "uid": "body", "data_type": "json", "multiple": false, "non_localizable": false }
        ]
        """);

    private static JObject Embed(string displayType) => new()
    {
        ["uid"] = $"embed-{displayType}",
        ["type"] = "reference",
        ["attrs"] = new JObject
        {
            ["display-type"] = displayType,
            ["type"] = "entry",
            ["class-name"] = $"embedded-entry redactor-component {displayType}-entry",
            ["entry-uid"] = "price-entry-1",
            ["locale"] = "en-us",
            ["content-type-uid"] = "pricing"
        },
        ["children"] = new JArray(new JObject { ["text"] = "" })
    };

    private static JObject SourceEntry() => new()
    {
        ["title"] = "Pricing",
        ["body"] = new JObject
        {
            ["type"] = "doc",
            ["children"] = new JArray(
                new JObject
                {
                    ["type"] = "p",
                    ["children"] = new JArray(
                        new JObject { ["text"] = "Payroll starts at " },
                        Embed("inline"),
                        new JObject { ["text"] = " per employee." })
                },
                Embed("block"))
        }
    };

    private static JObject Text(string text) => new() { ["text"] = text };

    private static JObject PricingParagraphEntry() => new()
    {
        ["title"] = "Pricing",
        ["body"] = new JObject
        {
            ["type"] = "doc",
            ["children"] = new JArray(new JObject
            {
                ["type"] = "p",
                ["children"] = new JArray(
                    Text("Plan A starts at "),
                    Embed("inline"),
                    Text(" per user per month. Plan B starts at $5 per user, Plan C at "),
                    Embed("inline"),
                    Text(" per seat, and Plan D at "),
                    Embed("inline"),
                    Text(" per user. Compare every plan on our "),
                    new JObject
                    {
                        ["type"] = "a",
                        ["attrs"] = new JObject { ["url"] = "https://example.com/pricing" },
                        ["children"] = new JArray(Text("pricing"))
                    },
                    Text(" page."))
            })
        }
    };

    private static string Download(JObject entry) => Encoding.UTF8.GetString(
        JsonToHtmlConverter.ToHtml(entry, new ContentTypeBlockEntity { Schema = Schema }, null,
            "landing_page", EntryId, "stackapikey", null));

    private static Transformation Parse(string html)
    {
        var transformation = Transformation.Parse(html, "entry.html");
        transformation.SourceLanguage = "en-us";
        transformation.TargetLanguage = "fr-fr";
        return transformation;
    }

    private static HtmlDocument LoadHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    private static string ParagraphPath => "body.children[0]";

    [TestMethod]
    public void Download_InlineEmbed_IsAnEmptyKeyedSpanInsideTheParagraph()
    {
        var doc = LoadHtml(Download(SourceEntry()));

        var paragraph = doc.DocumentNode.SelectSingleNode("//p")!;
        var embed = paragraph.SelectSingleNode($"./span[@data-blackbird-key='{EntryId}-{ParagraphPath}.children[1]']");

        Assert.IsNotNull(embed, "the inline embed must stay inside the paragraph as a span: " + paragraph.OuterHtml);
        Assert.AreEqual(string.Empty, embed.InnerHtml);
        Assert.IsNull(paragraph.SelectSingleNode(".//div"), "a div inside a paragraph splits it: " + paragraph.OuterHtml);
    }

    [TestMethod]
    public void Download_BlockEmbed_StaysAKeyedDiv()
    {
        var doc = LoadHtml(Download(SourceEntry()));

        var embed = doc.DocumentNode.SelectSingleNode($"//div[@data-blackbird-key='{EntryId}-body.children[1]']");

        Assert.IsNotNull(embed, doc.DocumentNode.OuterHtml);
        Assert.AreEqual(string.Empty, embed.InnerHtml);
    }

    [TestMethod]
    public void Download_ParagraphWithInlineEmbed_IsOneUnitThatKeepsTheSpacesAroundTheEmbed()
    {
        var units = Parse(Download(SourceEntry())).GetUnits()
            .Where(x => x.Segments.Any(s => !string.IsNullOrWhiteSpace(s.GetSource())))
            .ToList();

        var payrollUnits = units.Where(x => x.Segments.Any(s => s.GetSource().Contains("Payroll"))).ToList();

        Assert.AreEqual(1, payrollUnits.Count,
            "the paragraph must be one unit: " + string.Join(" | ", units.Select(x => x.Segments[0].GetSource())));

        Assert.AreEqual($"{EntryId}-{ParagraphPath}", payrollUnits[0].Key);

        var source = string.Concat(payrollUnits[0].Segments.Select(x => x.GetSource()));
        StringAssert.Contains(source, "per employee.");
        Assert.IsTrue(source.Contains("at <") && source.Contains("> per"),
            "the spaces next to the embed must stay inside the unit: " + source);
    }

    [TestMethod]
    public void Download_ParagraphWithSeveralInlineEmbedsAndALink_IsOneUnit()
    {
        var units = Parse(Download(PricingParagraphEntry())).GetUnits()
            .Where(x => x.Segments.Any(s => s.GetSource().Contains("per ")))
            .ToList();

        Assert.AreEqual(1, units.Count,
            "the paragraph must be one unit: " + string.Join(" | ", units.Select(x => x.Segments[0].GetSource())));
        Assert.AreEqual($"{EntryId}-{ParagraphPath}", units[0].Key);
    }

    [TestMethod]
    public void Upload_ReorderedTranslationOfParagraphWithSeveralInlineEmbeds_KeepsEverySpace()
    {
        var transformation = Parse(Download(PricingParagraphEntry()));

        foreach (var segment in transformation.GetUnits().SelectMany(x => x.Segments))
            segment.SetTarget(segment.GetSource()
                .Replace("Plan A starts at ", "Plan A à partir de ")
                .Replace(" per user per month. Plan B starts at $5 per user, Plan C at ",
                    " par utilisateur et par mois. Plan B à partir de 5 $ par utilisateur, Plan C à ")
                .Replace(" per seat, and Plan D at ", " par poste, et Plan D à ")
                .Replace(" per user. Compare every plan on our ", " par utilisateur. Comparez les plans sur notre page de ")
                .Replace(">pricing<", ">tarifs<")
                .Replace(" page.", "."));

        var target = Transformation.Parse(transformation.Serialize(), "entry.xliff").Target().Serialize()!;

        var entry = PricingParagraphEntry();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(target));
        var report = HtmlToJsonConverter.UpdateEntryFromHtml(stream, entry, null);

        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));

        var texts = entry.SelectToken(ParagraphPath)!.SelectTokens("$..text").Select(x => x.Value<string>()).ToList();

        CollectionAssert.AreEqual(new[]
        {
            "Plan A à partir de ", "",
            " par utilisateur et par mois. Plan B à partir de 5 $ par utilisateur, Plan C à ", "",
            " par poste, et Plan D à ", "",
            " par utilisateur. Comparez les plans sur notre page de ", "tarifs", "."
        }, texts, string.Join(" | ", texts));
    }

    [TestMethod]
    public void Upload_TranslatedParagraphWithInlineEmbed_KeepsTheSpacesAroundTheEmbed()
    {
        var transformation = Parse(Download(SourceEntry()));

        foreach (var segment in transformation.GetUnits().SelectMany(x => x.Segments))
            segment.SetTarget(segment.GetSource()
                .Replace("Payroll starts at", "La paie à partir de")
                .Replace("per employee.", "par employé."));

        var target = Transformation.Parse(transformation.Serialize(), "entry.xliff").Target().Serialize()!;

        var entry = SourceEntry();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(target));
        var report = HtmlToJsonConverter.UpdateEntryFromHtml(stream, entry, null);

        Assert.AreEqual(0, report.Errors.Count, string.Join(" | ", report.Errors));

        var paragraph = entry.SelectToken(ParagraphPath)!;
        Assert.AreEqual("La paie à partir de ", paragraph.SelectToken("children[0].text")!.Value<string>());
        Assert.AreEqual(" par employé.", paragraph.SelectToken("children[2].text")!.Value<string>());
        Assert.IsTrue(JToken.DeepEquals(Embed("inline"), paragraph.SelectToken("children[1]")));
        Assert.IsTrue(JToken.DeepEquals(Embed("block"), entry.SelectToken("body.children[1]")));
    }
}
