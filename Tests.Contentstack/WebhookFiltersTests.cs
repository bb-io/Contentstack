using Apps.Contentstack.Models.Request.ContentType;
using Apps.Contentstack.Webhooks;
using Blackbird.Applications.Sdk.Common.Webhooks;
using Newtonsoft.Json;

namespace Tests.Contentstack;

[TestClass]
public class WebhookFiltersTests
{
    private static WebhookRequest BuildRequest(string locale, string? updatedBy = null, string contentTypeUid = "page",
        string? environmentUid = null, string? branchUid = null)
    {
        var body = JsonConvert.SerializeObject(new
        {
            data = new
            {
                entry = new
                {
                    uid = "blt_test_entry",
                    locale,
                    title = "Test Entry",
                    tags = Array.Empty<string>(),
                    created_at = "2024-01-01T00:00:00.000Z",
                    updated_by = updatedBy
                },
                content_type = new
                {
                    uid = contentTypeUid,
                    title = "Page",
                    description = "",
                    created_at = "2024-01-01T00:00:00.000Z"
                },
                environment = environmentUid is null ? null : new
                {
                    uid = environmentUid,
                    name = "Test environment"
                },
                branch = branchUid is null ? null : new
                {
                    uid = branchUid
                }
            }
        });

        return new WebhookRequest { Body = body };
    }

    // --- Branch ---

    [TestMethod]
    public async Task OnEntryPublished_BranchMatches_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest { Branch = "main" };

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us", branchUid: "MAIN"), filter);

        Assert.AreNotEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNotNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryPublished_BranchDoesNotMatch_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest { Branch = "main" };

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us", branchUid: "development"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryPublished_BranchMissingFromPayload_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest { Branch = "main" };

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryPublished_BranchNotSet_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest();

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us", branchUid: "development"), filter);

        Assert.AreNotEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNotNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryPublished_EnvironmentMatchesButBranchDoesNotMatch_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest
        {
            Environment = "blt_environment",
            Branch = "main"
        };

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us",
            environmentUid: "blt_environment", branchUid: "development"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryPublished_BranchMatchesButEnvironmentDoesNotMatch_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new EntryPublishedOptionalRequest
        {
            Environment = "blt_production",
            Branch = "main"
        };

        var result = await webhookList.OnEntryPublished(BuildRequest("en-us",
            environmentUid: "blt_staging", branchUid: "main"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    // --- ExcludeLocale ---

    [TestMethod]
    public async Task OnEntryUpdated_ExcludeLocaleMatches_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { ExcludeLocale = "en-us" };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryUpdated_ExcludeLocaleMatchesCaseInsensitive_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { ExcludeLocale = "EN-US" };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }

    [TestMethod]
    public async Task OnEntryUpdated_ExcludeLocaleDoesNotMatch_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { ExcludeLocale = "en-us" };

        var result = await webhookList.OnEntryUpdated(BuildRequest("fr-fr"), filter);

        Assert.AreNotEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNotNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryUpdated_ExcludeLocaleNotSet_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest();

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us"), filter);

        Assert.IsNotNull(result.Result);
    }

    // --- UpdatedByUserId (include filter) ---

    [TestMethod]
    public async Task OnEntryUpdated_UpdatedByUserIdMatches_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { UpdatedByUserId = "bltuser123" };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser123"), filter);

        Assert.AreNotEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNotNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryUpdated_UpdatedByUserIdDoesNotMatch_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { UpdatedByUserId = "bltuser123" };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser456"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryUpdated_UpdatedByUserIdNotSet_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest();

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser123"), filter);

        Assert.IsNotNull(result.Result);
    }

    // --- NotUpdatedByUserId (exclude filter) ---

    [TestMethod]
    public async Task OnEntryUpdated_NotUpdatedByUserIdMatches_ReturnsPreflight()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { NotUpdatedByUserId = ["bltuser123"] };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser123"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task OnEntryUpdated_NotUpdatedByUserIdDoesNotMatch_ReturnsResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest { NotUpdatedByUserId = ["bltuser123"] };

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser456"), filter);

        Assert.AreNotEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
        Assert.IsNotNull(result.Result);
    }

    // --- UpdatedByUserId output field ---

    [TestMethod]
    public async Task OnEntryUpdated_WithUpdatedBy_ExposesUpdatedByUserIdInResult()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest();

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser123"), filter);

        Assert.IsNotNull(result.Result);
        Assert.AreEqual("bltuser123", result.Result.UpdatedByUserId);
    }

    [TestMethod]
    public async Task OnEntryUpdated_WithoutUpdatedBy_UpdatedByUserIdIsNull()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest();

        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: null), filter);

        Assert.IsNotNull(result.Result);
        Assert.IsNull(result.Result.UpdatedByUserId);
    }

    // --- Combined filters ---

    [TestMethod]
    public async Task OnEntryUpdated_ExcludeLocaleAndUpdatedByBothApply_ExcludeLocaleTakesPrecedence()
    {
        var webhookList = new WebhookList();
        var filter = new ContentTypeOptionalRequest
        {
            ExcludeLocale = "en-us",
            UpdatedByUserId = "bltuser123"
        };

        // Locale matches exclude → preflight, even though UpdatedBy also matches
        var result = await webhookList.OnEntryUpdated(BuildRequest("en-us", updatedBy: "bltuser123"), filter);

        Assert.AreEqual(WebhookRequestType.Preflight, result.ReceivedWebhookRequestType);
    }
}
