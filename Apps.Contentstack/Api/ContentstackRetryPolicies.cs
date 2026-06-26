using System.Globalization;
using System.Net;
using RestSharp;

namespace Apps.Contentstack.Api;

public static class ContentstackRetryPolicies
{
    private const int RetryCount = 5;
    private const double BaseDelaySeconds = 1;
    private const double MaxDelaySeconds = 30;
    private const double JitterMin = 0.8;
    private const double JitterMax = 1.2;

    public static async Task<RestResponse> ExecuteWithRateLimitRetry(Func<Task<RestResponse>> executeAsync,
        int retryCount = RetryCount)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await executeAsync();
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt > retryCount)
            {
                return response;
            }

            await Task.Delay(GetRetryDelay(attempt, response));
        }
    }

    internal static TimeSpan GetRetryDelay(int attempt, RestResponse response)
    {
        var delay = GetRetryAfterDelay(response)
            ?? GetRateLimitResetDelay(response)
            ?? GetExponentialDelay(attempt);

        var jitterMultiplier = Random.Shared.NextDouble() * (JitterMax - JitterMin) + JitterMin;
        var jitteredDelay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitterMultiplier);

        return jitteredDelay > TimeSpan.Zero ? jitteredDelay : TimeSpan.Zero;
    }

    private static TimeSpan? GetRetryAfterDelay(RestResponse response)
    {
        var headerValue = GetHeaderValue(response, "Retry-After");
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        if (double.TryParse(headerValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(Math.Max(0, seconds));
        }

        if (DateTimeOffset.TryParse(headerValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                out var retryAt))
        {
            return retryAt - DateTimeOffset.UtcNow;
        }

        return null;
    }

    private static TimeSpan? GetRateLimitResetDelay(RestResponse response)
    {
        var headerValue = GetHeaderValue(response, "X-RateLimit-Reset");
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        if (long.TryParse(headerValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue))
        {
            if (numericValue > 1_000_000_000)
            {
                var resetAt = DateTimeOffset.FromUnixTimeSeconds(numericValue);
                return resetAt - DateTimeOffset.UtcNow;
            }

            return TimeSpan.FromSeconds(Math.Max(0, numericValue));
        }

        if (DateTimeOffset.TryParse(headerValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                out var resetDate))
        {
            return resetDate - DateTimeOffset.UtcNow;
        }

        return null;
    }

    private static TimeSpan GetExponentialDelay(int attempt)
    {
        var delaySeconds = Math.Min(BaseDelaySeconds * Math.Pow(2, attempt - 1), MaxDelaySeconds);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private static string? GetHeaderValue(RestResponse response, string headerName)
    {
        return response.Headers?
            .FirstOrDefault(header => string.Equals(header.Name, headerName, StringComparison.OrdinalIgnoreCase))
            ?.Value?.ToString();
    }
}
