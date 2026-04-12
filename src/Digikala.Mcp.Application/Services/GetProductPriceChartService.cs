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
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_price_chart", ("productId", productId));
        try
        {
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
                cache,
                cacheKey,
                CacheTier.ShortTerm,
                async token =>
                {
                    using var response = await apiClient.GetProductPriceChartAsync(productId, ct: token);
                    return mapper.MapToPriceChart(productId, response);
                },
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new PriceChartDto
            {
                ProductId = productId,
                Warning = "Digikala product price chart request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_product_price_chart",
                    "Digikala product price chart request failed.",
                    exception,
                    request),
            };
        }
    }
}
