using System.Text.RegularExpressions;
using Digikala.Mcp.Application.Dtos;

namespace Digikala.Mcp.Application.Internal;

internal static partial class ToolIssueFactory
{
    public static ToolIssueDto Create(string tool, string message, Exception exception, Dictionary<string, string?> request)
    {
        var diagnostics = new List<string> { exception.Message };
        var statusCode = TryGetStatusCode(exception.Message);
        var endpoint = TryGetEndpoint(exception.Message);

        return new ToolIssueDto
        {
            Code = GetCode(exception, statusCode),
            Message = message,
            Tool = tool,
            Request = request,
            UpstreamEndpoint = endpoint,
            StatusCode = statusCode,
            Retryable = IsRetryable(exception, statusCode),
            Diagnostics = diagnostics,
        };
    }

    private static string GetCode(Exception exception, int? statusCode)
    {
        if (exception is TimeoutException)
        {
            return "UpstreamTimeout";
        }

        if (exception.Message.Contains("Circuit breaker is open", StringComparison.OrdinalIgnoreCase))
        {
            return "CircuitOpen";
        }

        return statusCode switch
        {
            404 => "NotFound",
            429 => "RateLimited",
            >= 500 => "UpstreamError",
            _ => "UpstreamError",
        };
    }

    private static bool IsRetryable(Exception exception, int? statusCode)
    {
        if (exception is TimeoutException)
        {
            return true;
        }

        if (exception.Message.Contains("Circuit breaker is open", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return statusCode is >= 500 or 429;
    }

    private static int? TryGetStatusCode(string message)
    {
        var match = StatusRegex().Match(message);
        return match.Success && int.TryParse(match.Groups["status"].Value, out var status)
            ? status
            : null;
    }

    private static string? TryGetEndpoint(string message)
    {
        var match = UrlRegex().Match(message);
        if (!match.Success)
        {
            return null;
        }

        return Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var uri)
            ? uri.PathAndQuery
            : match.Groups["url"].Value;
    }

    [GeneratedRegex(@"HTTP\s+(?<status>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"for\s+(?<url>https?://\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}
