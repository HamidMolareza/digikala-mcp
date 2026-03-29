using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using System.Collections;
using System.Reflection;

namespace Digikala.Mcp.Application.Internal;

internal static class ToolExecutionHelper
{
    public static async Task<T> GetOrCreateAsync<T>(
        ICacheStore cache,
        string cacheKey,
        CacheTier tier,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken ct = default)
        where T : class
    {
        var cached = await cache.GetAsync<T>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        try
        {
            var value = await factory(ct);
            await cache.SetAsync(cacheKey, value, tier, ct);
            return value;
        }
        catch
        {
            var stale = await cache.GetStaleAsync<T>(cacheKey, ct);
            if (stale is not null)
            {
                MarkAsStale(stale);
                return stale;
            }

            throw;
        }
    }

    public static string CreateCacheKey(string toolName, params (string Key, object? Value)[] parts)
    {
        var normalized = parts
            .Where(part => part.Value is not null && !string.IsNullOrWhiteSpace(part.Value.ToString()))
            .OrderBy(part => part.Key, StringComparer.Ordinal)
            .Select(part => $"{part.Key}={Normalize(part.Value)}");

        return $"{toolName}:{string.Join('|', normalized)}";
    }

    private static string Normalize(object? value) =>
        value switch
        {
            null => string.Empty,
            string text => text.Trim(),
            IEnumerable<string> items => string.Join(',', items.Select(item => item.Trim())),
            _ => value.ToString()?.Trim() ?? string.Empty,
        };

    private static void MarkAsStale<T>(T value) where T : class
    {
        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    MarkObjectAsStale(item);
                }
            }

            return;
        }

        MarkObjectAsStale(value);
    }

    private static void MarkObjectAsStale(object value)
    {
        SetPropertyIfPresent(value, "IsStale", true);
        SetPropertyIfPresent(value, "Warning", "Served from stale cache because the upstream request failed.");
    }

    private static void SetPropertyIfPresent(object target, string propertyName, object propertyValue)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite || property.PropertyType != propertyValue.GetType())
        {
            return;
        }

        property.SetValue(target, propertyValue);
    }
}
