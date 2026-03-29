using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Infrastructure.Cache;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digikala.Mcp.Infrastructure.Tests;

public class SqliteCacheStoreTests : IAsyncDisposable
{
    private readonly SqliteCacheStore _cache;
    private readonly string _dbPath;

    public SqliteCacheStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid():N}.db");
        var options = Options.Create(new CacheOptions { DatabasePath = _dbPath });
        _cache = new SqliteCacheStore(options, NullLogger<SqliteCacheStore>.Instance);
    }

    [Fact]
    public async Task SetAndGet_ShouldRoundTrip()
    {
        var data = new TestData("hello", 42);
        await _cache.SetAsync("test-key", data, CacheTier.ShortTerm);

        var result = await _cache.GetAsync<TestData>("test-key");

        Assert.NotNull(result);
        Assert.Equal("hello", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_ForMissingKey()
    {
        var result = await _cache.GetAsync<TestData>("missing-key");
        Assert.Null(result);
    }

    [Fact]
    public async Task Remove_ShouldDeleteEntry()
    {
        var data = new TestData("to-delete", 1);
        await _cache.SetAsync("del-key", data, CacheTier.LongTerm);
        await _cache.RemoveAsync("del-key");

        var result = await _cache.GetAsync<TestData>("del-key");
        Assert.Null(result);
    }

    [Fact]
    public async Task CleanupExpired_ShouldNotDeleteLiveEntries()
    {
        var data = new TestData("alive", 1);
        await _cache.SetAsync("live-key", data, CacheTier.LongTerm);
        await _cache.CleanupExpiredAsync();

        var result = await _cache.GetAsync<TestData>("live-key");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetStale_ShouldReturnExpiredEntry()
    {
        var data = new TestData("stale", 9);
        await _cache.SetAsync("stale-key", data, CacheTier.ShortTerm);

        await using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE cache_entries SET expires_at = @expiresAt WHERE key = @key";
            command.Parameters.AddWithValue("@expiresAt", DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds());
            command.Parameters.AddWithValue("@key", "stale-key");
            await command.ExecuteNonQueryAsync();
        }

        var fresh = await _cache.GetAsync<TestData>("stale-key");
        var stale = await _cache.GetStaleAsync<TestData>("stale-key");

        Assert.Null(fresh);
        Assert.NotNull(stale);
        Assert.Equal("stale", stale.Name);
    }

    public async ValueTask DisposeAsync()
    {
        await _cache.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private sealed record TestData(string Name, int Value);
}
