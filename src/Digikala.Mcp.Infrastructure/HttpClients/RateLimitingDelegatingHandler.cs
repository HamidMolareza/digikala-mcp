using System.Threading.RateLimiting;
using Digikala.Mcp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Digikala.Mcp.Infrastructure.HttpClients;

public sealed class RateLimitingDelegatingHandler : DelegatingHandler
{
    private readonly TokenBucketRateLimiter _rateLimiter;

    public RateLimitingDelegatingHandler(IOptions<DigikalaOptions> options)
    {
        var permitLimit = Math.Max(1, options.Value.MaxRequestsPerSecond);
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            TokenLimit = permitLimit,
            TokensPerPeriod = permitLimit,
            QueueLimit = permitLimit * 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            throw new HttpRequestException("Client-side rate limiter rejected the request.");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rateLimiter.Dispose();
        }

        base.Dispose(disposing);
    }
}
