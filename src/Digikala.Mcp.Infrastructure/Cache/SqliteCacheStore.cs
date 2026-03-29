using System.Text.Json;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digikala.Mcp.Infrastructure.Cache;

public sealed class SqliteCacheStore : ICacheStore, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CacheOptions _options;
    private readonly ILogger<SqliteCacheStore> _logger;

    public SqliteCacheStore(IOptions<CacheOptions> options, ILogger<SqliteCacheStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        var directory = Path.GetDirectoryName(Path.GetFullPath(_options.DatabasePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        _connection = new SqliteConnection($"Data Source={_options.DatabasePath}");
        _connection.Open();
        InitializeSchema();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        const string sql = "SELECT value FROM cache_entries WHERE key = @key AND expires_at > @now";

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is not byte[] bytes)
        {
            _logger.LogDebug("Cache MISS: {Key}", key);
            return null;
        }

        _logger.LogDebug("Cache HIT: {Key}", key);
        return JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task<T?> GetStaleAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        const string sql = "SELECT value FROM cache_entries WHERE key = @key";

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@key", key);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is not byte[] bytes)
        {
            return null;
        }

        _logger.LogWarning("Serving stale cache entry for {Key}", key);
        return JsonSerializer.Deserialize<T>(bytes);
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

        const string sql = """
            INSERT OR REPLACE INTO cache_entries (key, value, tier, expires_at, created_at)
            VALUES (@key, @value, @tier, @expiresAt, @createdAt)
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", bytes);
        cmd.Parameters.AddWithValue("@tier", tier.ToString());
        cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
        cmd.Parameters.AddWithValue("@createdAt", now);

        await cmd.ExecuteNonQueryAsync(ct);
        _logger.LogDebug("Cache SET: {Key} tier={Tier} ttl={Ttl}m", key, tier, ttlMinutes);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM cache_entries WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM cache_entries WHERE tier = @tier";
        cmd.Parameters.AddWithValue("@tier", tier.ToString());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CleanupExpiredAsync(CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM cache_entries WHERE expires_at <= @now";
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var deleted = await cmd.ExecuteNonQueryAsync(ct);
        if (deleted > 0)
            _logger.LogDebug("Cache cleanup: removed {Count} expired entries", deleted);
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;

            CREATE TABLE IF NOT EXISTS cache_entries (
                key TEXT PRIMARY KEY,
                value BLOB NOT NULL,
                tier TEXT NOT NULL,
                expires_at INTEGER NOT NULL,
                created_at INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_cache_expires ON cache_entries(expires_at);
            CREATE INDEX IF NOT EXISTS idx_cache_tier ON cache_entries(tier);
            """;
        cmd.ExecuteNonQuery();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
