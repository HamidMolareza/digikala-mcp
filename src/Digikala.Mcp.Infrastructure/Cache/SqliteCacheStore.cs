using System.Text.Json;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digikala.Mcp.Infrastructure.Cache;

public sealed class SqliteCacheStore : ICacheStore
{
    private readonly CacheOptions _options;
    private readonly ILogger<SqliteCacheStore> _logger;
    private readonly IDbContextFactory<CacheDbContext> _dbContextFactory;

    public SqliteCacheStore(
        IOptions<CacheOptions> options,
        IDbContextFactory<CacheDbContext> dbContextFactory,
        ILogger<SqliteCacheStore> logger)
    {
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        var directory = Path.GetDirectoryName(Path.GetFullPath(_options.DatabasePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var context = _dbContextFactory.CreateDbContext();
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        var entry = await context.CacheEntries
            .AsNoTracking()
            .Where(item => item.Key == key && item.ExpiresAt > now)
            .Select(item => new { item.Key, item.Value })
            .SingleOrDefaultAsync(ct);

        if (entry is null)
        {
            _logger.LogDebug("Cache MISS: {Key}", key);
            return null;
        }

        try
        {
            _logger.LogDebug("Cache HIT: {Key}", key);
            return JsonSerializer.Deserialize<T>(entry.Value);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            _logger.LogWarning(exception, "Discarding unreadable cache entry for {Key}", key);
            await RemoveAsync(entry.Key, ct);
            return null;
        }
    }

    public async Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        var entry = await context.CacheEntries
            .AsNoTracking()
            .Where(item => item.Key == key)
            .Select(item => new { item.Key, item.Value })
            .SingleOrDefaultAsync(ct);

        if (entry is null)
        {
            return null;
        }

        try
        {
            _logger.LogWarning("Serving stale cache entry for {Key}", key);
            return JsonSerializer.Deserialize<T>(entry.Value);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            _logger.LogWarning(exception, "Discarding unreadable stale cache entry for {Key}", key);
            await RemoveAsync(entry.Key, ct);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken ct = default) where T : class
    {
        if (tier == CacheTier.None) return;

        var ttlMinutes = tier switch
        {
            CacheTier.ShortTerm => _options.ShortTermTtlMinutes,
            CacheTier.MediumTerm => _options.MediumTermTtlMinutes,
            CacheTier.LongTerm => _options.LongTermTtlMinutes,
            _ => 0,
        };

        if (ttlMinutes <= 0) return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = now + (ttlMinutes * 60);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existing = await context.CacheEntries.SingleOrDefaultAsync(item => item.Key == key, ct);
            if (existing is null)
            {
                context.CacheEntries.Add(new CacheEntry
                {
                    Key = key,
                    Value = bytes,
                    Tier = tier.ToString(),
                    ExpiresAt = expiresAt,
                    CreatedAt = now,
                });
            }
            else
            {
                existing.Value = bytes;
                existing.Tier = tier.ToString();
                existing.ExpiresAt = expiresAt;
                existing.CreatedAt = now;
            }

            await context.SaveChangesAsync(ct);
            _logger.LogDebug("Cache SET: {Key} tier={Tier} ttl={Ttl}m", key, tier, ttlMinutes);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cache SET failed for {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        await context.CacheEntries
            .Where(item => item.Key == key)
            .ExecuteDeleteAsync(ct);
    }

    public async Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        await context.CacheEntries
            .Where(item => item.Tier == tier.ToString())
            .ExecuteDeleteAsync(ct);
    }

    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
        var deleted = await context.CacheEntries
            .Where(item => item.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _logger.LogDebug("Cache cleanup: removed {Count} expired entries", deleted);
    }
}
