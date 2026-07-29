using System.Net;
using System.Text.RegularExpressions;
using Apps.Contentstack.Constants;
using Apps.Contentstack.Models.Response;
using Blackbird.Applications.Sdk.Common.Exceptions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Apps.Contentstack.Api;

public static class ContentstackErrorParser
{
    private const int MaxBodyLength = 400;
    private const int MaxParsedBodyLength = 8000;

    public static Exception BuildException(HttpStatusCode statusCode, string? content, string? transportErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new PluginApplicationException(string.IsNullOrWhiteSpace(transportErrorMessage)
                ? $"Contentstack returned {DescribeStatus(statusCode)} without a response body."
                : $"Contentstack returned {DescribeStatus(statusCode)}: {transportErrorMessage}");
        }

        if (TryParseError(content, out var error))
        {
            if (error!.Errors is not null && error.Errors.TryGetValue("title", out var titleErrors))
            {
                var detail = string.Join("; ", titleErrors.Select(x => x.ToString()));
                return new PluginMisconfigurationException($"Field Title incorrect: {detail}");
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(error.ErrorMessage))
                parts.Add(error.ErrorMessage);
            if (error.Errors is not null)
                parts.Add(error.Errors.ToString());

            return new PluginApplicationException(string.Join("; ", parts));
        }

        var body = Summarize(content);
        
        return IsClientError(statusCode)
            ? new PluginMisconfigurationException(
                $"Contentstack returned {DescribeStatus(statusCode)} with a non-JSON response. " +
                $"Check that the 'Host' connection field points at the Contentstack management API and that the entry exists. Response: {body}")
            : new PluginApplicationException(
                $"Contentstack returned {DescribeStatus(statusCode)} with a non-JSON response: {body}");
    }

    public static string BuildParseFailureMessage(Type targetType, string? content)
        => $"Could not parse the Contentstack response as {targetType.Name}. Response: {Summarize(content)}";
    
    public static string Summarize(string? content, int maxLength = MaxBodyLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "<empty>";

        var text = content.Length > MaxParsedBodyLength
            ? content[..MaxParsedBodyLength]
            : content;

        if (LooksLikeMarkup(text))
            text = ExtractText(text);

        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length == 0)
            return "<empty>";

        return text.Length > maxLength
            ? text[..maxLength] + "…"
            : text;
    }

    private static bool TryParseError(string content, out ErrorResponse? error)
    {
        error = null;

        if (!LooksLikeJsonObject(content))
            return false;

        try
        {
            error = JsonConvert.DeserializeObject<ErrorResponse>(content, JsonConfig.Settings);
        }
        catch (JsonException)
        {
            return false;
        }

        if (error is null)
            return false;

        return !string.IsNullOrWhiteSpace(error.ErrorMessage) || error.Errors is not null;
    }

    private static bool LooksLikeJsonObject(string content)
        => content.TrimStart().StartsWith('{');

    private static bool LooksLikeMarkup(string content)
        => content.TrimStart().StartsWith('<');

    private static string ExtractText(string html)
    {
        try
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            foreach (var node in document.DocumentNode
                         .SelectNodes("//script|//style")?.ToList() ?? [])
            {
                node.Remove();
            }

            return HtmlEntity.DeEntitize(document.DocumentNode.InnerText) ?? html;
        }
        catch
        {
            return Regex.Replace(html, "<.*?>", " ");
        }
    }

    private static bool IsClientError(HttpStatusCode statusCode)
        => (int)statusCode is >= 400 and < 500;

    private static string DescribeStatus(HttpStatusCode statusCode)
        => (int)statusCode == 0
            ? "no HTTP status"
            : $"HTTP {(int)statusCode} ({statusCode})";
}
