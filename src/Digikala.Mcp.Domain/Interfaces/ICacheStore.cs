using Digikala.Mcp.Domain.Enums;

namespace Digikala.Mcp.Domain.Interfaces;

public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    Task SetAsync<T>(string key, T value, CacheTier tier, CancellationToken ct = default) where T : class;

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task RemoveByTierAsync(CacheTier tier, CancellationToken ct = default);

    Task CleanupExpiredAsync(CancellationToken ct = default);
}
