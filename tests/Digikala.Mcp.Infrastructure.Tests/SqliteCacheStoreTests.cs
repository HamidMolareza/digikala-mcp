using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Infrastructure.Cache;
using Digikala.Mcp.Infrastructure.Configuration;
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

    public async ValueTask DisposeAsync()
    {
        await _cache.DisposeAsync();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private sealed record TestData(string Name, int Value);
}
