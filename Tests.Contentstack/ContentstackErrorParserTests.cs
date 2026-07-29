using System.Net;
using Apps.Contentstack.Api;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.Contentstack;

[TestClass]
public class ContentstackErrorParserTests
{
    private const string HtmlErrorPage = """
    <html>
      <head><title>502 Bad Gateway</title></head>
      <body>
        <h1>502 Bad Gateway</h1>
        <p>The server returned an invalid response.</p>
      </body>
    </html>
    """;

    // --- JSON error bodies keep working ---

    [TestMethod]
    public void BuildException_JsonErrorWithErrorMessage_ReturnsApplicationExceptionWithMessage()
    {
        var content = """{ "error_message": "The entry could not be found.", "error_code": 141 }""";

        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.UnprocessableEntity, content, null);

        Assert.IsInstanceOfType<PluginApplicationException>(exception);
        StringAssert.Contains(exception.Message, "The entry could not be found.");
    }

    [TestMethod]
    public void BuildException_JsonErrorWithTitleErrors_ReturnsMisconfigurationException()
    {
        var content = """{ "error_message": "Failed", "errors": { "title": ["is not unique."] } }""";

        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.UnprocessableEntity, content, null);

        Assert.IsInstanceOfType<PluginMisconfigurationException>(exception);
        StringAssert.Contains(exception.Message, "Field Title incorrect: is not unique.");
    }

    [TestMethod]
    public void BuildException_JsonErrorWithOtherErrors_IncludesErrorDetails()
    {
        var content = """{ "error_message": "Failed", "errors": { "api_key": ["is not valid."] } }""";

        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.Unauthorized, content, null);

        StringAssert.Contains(exception.Message, "Failed");
        StringAssert.Contains(exception.Message, "is not valid.");
    }

    [TestMethod]
    public void BuildException_JsonErrorWithoutErrorMessage_DoesNotStartWithSeparator()
    {
        var content = """{ "errors": { "api_key": ["is not valid."] } }""";

        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.Unauthorized, content, null);

        StringAssert.Contains(exception.Message, "is not valid.");
        Assert.IsFalse(exception.Message.TrimStart().StartsWith(';'),
            $"Message must not start with a dangling separator. Actual: {exception.Message}");
    }

    // --- The reported bug: non-JSON bodies ---

    [TestMethod]
    public void BuildException_HtmlBodyOnServerError_ReturnsApplicationExceptionWithStatusCode()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.BadGateway, HtmlErrorPage, null);

        Assert.IsInstanceOfType<PluginApplicationException>(exception);
        StringAssert.Contains(exception.Message, "502");
        StringAssert.Contains(exception.Message, "BadGateway");
    }

    [TestMethod]
    public void BuildException_HtmlBody_DoesNotLeakJsonParserNoise()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.BadGateway, HtmlErrorPage, null);

        Assert.IsFalse(exception.Message.Contains("Unexpected character", StringComparison.OrdinalIgnoreCase),
            $"The JSON parser error must not surface to the user. Actual: {exception.Message}");
        Assert.IsFalse(exception.Message.Contains("Path ''", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildException_HtmlBody_StripsMarkupAndCollapsesWhitespace()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.BadGateway, HtmlErrorPage, null);

        StringAssert.Contains(exception.Message, "The server returned an invalid response.");
        Assert.IsFalse(exception.Message.Contains("<h1>"), $"Markup must be stripped. Actual: {exception.Message}");
        Assert.IsFalse(exception.Message.Contains("  "), $"Whitespace must be collapsed. Actual: {exception.Message}");
    }

    [TestMethod]
    public void BuildException_HtmlBodyOnClientError_ReturnsMisconfigurationExceptionMentioningHost()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.NotFound, HtmlErrorPage, null);

        Assert.IsInstanceOfType<PluginMisconfigurationException>(exception);
        StringAssert.Contains(exception.Message, "404");
        StringAssert.Contains(exception.Message.ToLowerInvariant(), "host");
    }

    [TestMethod]
    public void BuildException_VeryLongBody_IsTruncated()
    {
        var content = "<p>" + new string('x', 5000) + "</p>";

        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.BadGateway, content, null);

        Assert.IsTrue(exception.Message.Length < 800, $"Message length was {exception.Message.Length}");
        StringAssert.Contains(exception.Message, "…");
    }

    [TestMethod]
    public void BuildException_PlainTextBody_ReturnsReadableMessage()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.ServiceUnavailable, "Service Unavailable", null);

        StringAssert.Contains(exception.Message, "503");
        StringAssert.Contains(exception.Message, "Service Unavailable");
    }

    // --- Malformed / unexpected JSON shapes must not throw ---

    [TestMethod]
    public void BuildException_JsonNullLiteral_DoesNotThrowAndMentionsStatus()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.InternalServerError, "null", null);

        StringAssert.Contains(exception.Message, "500");
    }

    [TestMethod]
    public void BuildException_JsonArrayBody_DoesNotThrowAndMentionsStatus()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.InternalServerError, "[1, 2, 3]", null);

        StringAssert.Contains(exception.Message, "500");
    }

    [TestMethod]
    public void BuildException_JsonObjectWithoutErrorFields_FallsBackToStatusMessage()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.InternalServerError, """{ "foo": 1 }""", null);

        StringAssert.Contains(exception.Message, "500");
    }

    // --- Empty bodies ---

    [TestMethod]
    public void BuildException_EmptyContent_UsesTransportErrorMessage()
    {
        var exception = ContentstackErrorParser.BuildException(0, null, "The connection was closed unexpectedly.");

        StringAssert.Contains(exception.Message, "The connection was closed unexpectedly.");
    }

    [TestMethod]
    public void BuildException_EmptyContentAndNoTransportMessage_StillMentionsStatus()
    {
        var exception = ContentstackErrorParser.BuildException(HttpStatusCode.GatewayTimeout, string.Empty, null);

        Assert.IsFalse(string.IsNullOrWhiteSpace(exception.Message));
        StringAssert.Contains(exception.Message, "504");
    }

    // --- Non-JSON success bodies ---

    [TestMethod]
    public void BuildParseFailureMessage_IncludesTargetTypeAndTruncatedBody()
    {
        var message = ContentstackErrorParser.BuildParseFailureMessage(typeof(string), HtmlErrorPage);

        StringAssert.Contains(message, "String");
        StringAssert.Contains(message, "502 Bad Gateway");
        Assert.IsFalse(message.Contains("<h1>"));
    }
}
