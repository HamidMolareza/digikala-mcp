using System.Text.Json;

namespace Digikala.Mcp.Application.Internal;

internal static class JsonHelpers
{
    public static JsonElement GetData(JsonDocument response) =>
        response.RootElement.TryGetProperty("data", out var data) ? data : response.RootElement;

    public static bool TryGetNested(this JsonElement element, out JsonElement value, params string[] path)
    {
        value = element;

        foreach (var segment in path)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    public static string? GetStringOrNull(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null,
        };
    }

    public static long? GetInt64OrNull(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var int64))
        {
            return int64;
        }

        if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out int64))
        {
            return int64;
        }

        return null;
    }

    public static int? GetInt32OrNull(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var int32))
        {
            return int32;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out int32))
        {
            return int32;
        }

        return null;
    }

    public static double? GetDoubleOrNull(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return null;
    }

    public static IEnumerable<JsonElement> EnumerateArrayOrEmpty(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray();
    }

    public static string? GetUrl(this JsonElement element, params string[] path)
    {
        if (!element.TryGetNested(out var value, path))
        {
            return null;
        }

        return GetUrl(value);
    }

    public static string? GetUrl(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("uri", out var uri) && uri.ValueKind == JsonValueKind.String)
            {
                return uri.GetString();
            }

            if (value.TryGetProperty("url", out var innerUrl))
            {
                return GetUrl(innerUrl);
            }
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                var url = GetUrl(item);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
        }

        return null;
    }

    public static DateTimeOffset ParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.UnixEpoch;
    }

    public static bool IsAvailableStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return !status.Contains("unavailable", StringComparison.OrdinalIgnoreCase) &&
               !status.Contains("out_of_stock", StringComparison.OrdinalIgnoreCase) &&
               !status.Contains("out-of-stock", StringComparison.OrdinalIgnoreCase) &&
               !status.Contains("inactive", StringComparison.OrdinalIgnoreCase);
    }
}
