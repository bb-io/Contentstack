using Apps.Contentstack.Constants;
using Apps.Contentstack.Helper;
using Apps.Contentstack.Models.Entities;
using Apps.Contentstack.Models.Response.Entry;
using Newtonsoft.Json;

namespace Tests.Contentstack;

[TestClass]
public class PublishDetailsFilterTests
{
    private const string Publisher = "user-publisher";
    private const string OtherUser = "user-other";
    private const string Production = "env-production";
    private const string Staging = "env-staging";

    private static EntryEntity BuildEntry(string entryLocale, params PublishDetailEntity[] publishDetails)
        => new() { ContentId = "entry-1", Locale = entryLocale, PublishDetails = publishDetails.ToList() };

    private static PublishDetailEntity PublishedBy(string user, string environment, string locale)
        => new() { User = user, Environment = environment, Locale = locale };

    private static ListEntriesResponse Deserialize(object publishDetails)
    {
        var json = JsonConvert.SerializeObject(new
        {
            entries = new[]
            {
                new Dictionary<string, object?>
                {
                    ["uid"] = "entry-1",
                    ["locale"] = "en-us",
                    ["title"] = "Entry",
                    ["publish_details"] = publishDetails
                }
            }
        });

        return JsonConvert.DeserializeObject<ListEntriesResponse>(json, JsonConfig.Settings)!;
    }

    [TestMethod]
    public void Deserialize_PublishDetailsArray_ReadsEveryRecord()
    {
        var response = Deserialize(new[]
        {
            new { environment = Production, locale = "en-us", user = Publisher, version = 1 },
            new { environment = Staging, locale = "fr-fr", user = OtherUser, version = 2 }
        });

        var details = response.Entries.Single().PublishDetails;
        Assert.HasCount(2, details);
        Assert.AreEqual(Production, details[0].Environment);
        Assert.AreEqual("en-us", details[0].Locale);
        Assert.AreEqual(Publisher, details[0].User);
        Assert.AreEqual("fr-fr", details[1].Locale);
    }

    [TestMethod]
    public void Deserialize_PublishDetailsSingleObject_ReadsItAsOneRecord()
    {
        var response = Deserialize(new { environment = Production, locale = "en-us", user = Publisher, time = "2026-01-01T00:00:00.000Z" });

        var details = response.Entries.Single().PublishDetails;
        Assert.HasCount(1, details);
        Assert.AreEqual(Production, details[0].Environment);
        Assert.AreEqual(Publisher, details[0].User);
    }

    [TestMethod]
    public void Deserialize_PublishDetailsMissing_ReturnsEmptyList()
    {
        var json = JsonConvert.SerializeObject(new { entries = new[] { new { uid = "entry-1", locale = "en-us" } } });

        var response = JsonConvert.DeserializeObject<ListEntriesResponse>(json, JsonConfig.Settings)!;

        Assert.IsEmpty(response.Entries.Single().PublishDetails);
    }

    [TestMethod]
    public void IsPublishedByAny_UserPublishedInSelectedEnvironmentAndLocale_ReturnsTrue()
    {
        var entry = BuildEntry("en-us", PublishedBy(Publisher, Production, "de-de"));

        Assert.IsTrue(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], Production, "de-de"));
    }

    [TestMethod]
    public void IsPublishedByAny_UserPublishedOnlyInAnotherEnvironment_ReturnsFalse()
    {
        var entry = BuildEntry("en-us",
            PublishedBy(Publisher, Staging, "en-us"),
            PublishedBy(OtherUser, Production, "en-us"));

        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], Production, "en-us"));
    }

    [TestMethod]
    public void IsPublishedByAny_UserPublishedOnlyInAnotherLocale_ReturnsFalse()
    {
        var entry = BuildEntry("en-us",
            PublishedBy(Publisher, Production, "fr-fr"),
            PublishedBy(OtherUser, Production, "de-de"));

        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], Production, "de-de"));
    }

    [TestMethod]
    public void IsPublishedByAny_NoEnvironmentSelected_MatchesAnyEnvironment()
    {
        var entry = BuildEntry("en-us", PublishedBy(Publisher, Staging, "en-us"));

        Assert.IsTrue(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], null, "en-us"));
    }

    [TestMethod]
    public void IsPublishedByAny_NoLocaleSelected_MatchesEntryLocale()
    {
        var entry = BuildEntry("en-us",
            PublishedBy(Publisher, Production, "fr-fr"),
            PublishedBy(OtherUser, Production, "en-us"));

        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], Production, null));
        Assert.IsTrue(PublishDetailsFilter.IsPublishedByAny(BuildEntry("fr-fr", PublishedBy(Publisher, Production, "fr-fr")),
            [Publisher], Production, null));
    }

    [TestMethod]
    public void IsPublishedByAny_PublishedByAnotherUser_ReturnsFalse()
    {
        var entry = BuildEntry("en-us", PublishedBy(OtherUser, Production, "en-us"));

        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher], Production, "en-us"));
    }

    [TestMethod]
    public void IsPublishedByAny_OneOfSeveralUsersPublished_ReturnsTrue()
    {
        var entry = BuildEntry("en-us", PublishedBy(OtherUser, Production, "en-us"));

        Assert.IsTrue(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher, OtherUser], Production, "en-us"));
    }

    [TestMethod]
    public void IsPublishedByAny_NoneOfSeveralUsersPublished_ReturnsFalse()
    {
        var entry = BuildEntry("en-us", PublishedBy("user-third", Production, "en-us"));

        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(entry, [Publisher, OtherUser], Production, "en-us"));
    }

    [TestMethod]
    public void IsPublishedByAny_NeverPublished_ReturnsFalse()
    {
        Assert.IsFalse(PublishDetailsFilter.IsPublishedByAny(BuildEntry("en-us"), [Publisher], Production, "en-us"));
    }
}
