using Digikala.Mcp.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Digikala.Mcp.Infrastructure.Cache;

public sealed class CacheCleanupService(
    ICacheStore cacheStore,
    IOptions<CacheOptions> options,
    ILogger<CacheCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes);
        logger.LogInformation("Cache cleanup service started, interval={Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await cacheStore.CleanupExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache cleanup failed");
            }
        }
    }
}
