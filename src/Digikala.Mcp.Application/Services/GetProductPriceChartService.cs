using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductPriceChartService(IDigikalaApiClient apiClient, ICacheStore cache, PriceChartMapper mapper)
{
    public async Task<PriceChartDto> ExecuteAsync(
        long productId,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_price_chart", ("productId", productId));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductPriceChartAsync(productId, ct: token);
                return mapper.MapToPriceChart(productId, response);
            },
            ct);
    }
}
