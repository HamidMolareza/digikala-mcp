using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Digikala.Mcp.Application.Internal;

internal static class ToolExecutionHelper
{
    public static async Task<T> GetOrCreateAsync<T>(
        ICacheStore cache,
        string cacheKey,
        CacheTier tier,
        Func<CancellationToken, Task<T>> factory,
        bool skipCache = false,
        CancellationToken ct = default)
        where T : class
    {
        if (skipCache)
        {
            var bypassedValue = await factory(ct);
            MarkWithCacheState(bypassedValue, isFromCache: false, cacheStatus: "bypassed");
            return bypassedValue;
        }

        T? cached = null;
        try
        {
            cached = await cache.GetAsync<T>(cacheKey, ct);
        }
        catch
        {
        }

        if (cached is not null)
        {
            MarkWithCacheState(cached, isFromCache: true, cacheStatus: "cache");
            return cached;
        }

        var value = await factory(ct);
        try
        {
            await cache.SetAsync(cacheKey, value, tier, ct);
        }
        catch
        {
        }

        MarkWithCacheState(value, isFromCache: false, cacheStatus: "live");
        return value;
    }

    public static async Task<T> GetOrCreateWithStaleFallbackAsync<T>(
        ICacheStore cache,
        string cacheKey,
        CacheTier tier,
        Func<CancellationToken, Task<T>> factory,
        bool skipCache = false,
        CancellationToken ct = default)
        where T : class
    {
        try
        {
            return await GetOrCreateAsync(cache, cacheKey, tier, factory, skipCache, ct);
        }
        catch
        {
            if (skipCache)
            {
                throw;
            }

            try
            {
                var stale = await cache.GetStaleAsync<T>(cacheKey, ct);
                if (stale is not null)
                {
                    MarkAsStale(stale);
                    MarkWithCacheState(stale, isFromCache: true, cacheStatus: "stale-cache");
                    return stale;
                }
            }
            catch
            {
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
            IEnumerable<string> items => string.Join(',',
                items
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)),
            IEnumerable items when value is not string => string.Join(',',
                items
                    .Cast<object?>()
                    .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.ToString()))
                    .Select(item => item is IFormattable formattable
                        ? formattable.ToString(null, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
                        : item!.ToString()!.Trim())
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty,
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

    private static void MarkWithCacheState<T>(T value, bool isFromCache, string cacheStatus) where T : class
    {
        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    SetCacheState(item, isFromCache, cacheStatus);
                }
            }

            return;
        }

        SetCacheState(value, isFromCache, cacheStatus);
    }

    private static void SetCacheState(object target, bool isFromCache, string cacheStatus)
    {
        SetPropertyIfPresent(target, "IsFromCache", isFromCache);
        SetPropertyIfPresent(target, "CacheStatus", cacheStatus);
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
