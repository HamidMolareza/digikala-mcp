using Digikala.Mcp.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace Digikala.Mcp.Server.Tools;

internal static class ToolOutcomeLogger
{
    public static async Task<T> ExecuteAsync<T>(ILogger logger, string toolName, Func<Task<T>> action)
    {
        var result = await action();
        Log(logger, toolName, result);
        return result;
    }

    private static void Log<T>(ILogger logger, string toolName, T? result)
    {
        if (result is null)
        {
            return;
        }

        var error = GetToolIssue(result);
        var warning = GetString(result, "Warning");
        var cacheStatus = GetString(result, "CacheStatus") ?? "unknown";
        var isStale = GetBool(result, "IsStale");

        if (error is not null)
        {
            var level = string.Equals(error.Code, "InvalidInput", StringComparison.OrdinalIgnoreCase)
                ? LogLevel.Warning
                : LogLevel.Error;

            logger.Log(
                level,
                "Tool {ToolName} returned structured issue: code={Code}, statusCode={StatusCode}, retryable={Retryable}, stale={IsStale}, cacheStatus={CacheStatus}, correlationId={CorrelationId}, message={Message}",
                toolName,
                error.Code,
                error.StatusCode,
                error.Retryable,
                isStale,
                cacheStatus,
                error.CorrelationId,
                error.Message);

            return;
        }

        if (!string.IsNullOrWhiteSpace(warning) || isStale)
        {
            logger.LogWarning(
                "Tool {ToolName} returned degraded response: stale={IsStale}, cacheStatus={CacheStatus}, warning={Warning}",
                toolName,
                isStale,
                cacheStatus,
                warning);
        }
    }

    private static ToolIssueDto? GetToolIssue<T>(T result)
    {
        var property = result?.GetType().GetProperty("Error");
        return property?.PropertyType == typeof(ToolIssueDto)
            ? property.GetValue(result) as ToolIssueDto
            : null;
    }

    private static string? GetString<T>(T result, string propertyName)
    {
        var property = result?.GetType().GetProperty(propertyName);
        return property?.PropertyType == typeof(string)
            ? property.GetValue(result) as string
            : null;
    }

    private static bool GetBool<T>(T result, string propertyName)
    {
        var property = result?.GetType().GetProperty(propertyName);
        return property?.PropertyType == typeof(bool)
            && property.GetValue(result) is bool value
            && value;
    }
}
