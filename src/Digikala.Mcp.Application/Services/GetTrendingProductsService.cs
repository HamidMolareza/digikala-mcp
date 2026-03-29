using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetTrendingProductsService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<List<ProductSummaryDto>> ExecuteAsync(CancellationToken ct = default)
    {
        const string cacheKey = "get_trending_products";
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetTrendingRecommendationAsync(token);
                var data = JsonHelpers.GetData(response);
                var products = data.TryGetNested(out var nestedProducts, "data", "products")
                    ? nestedProducts
                    : data.TryGetNested(out var directProducts, "products")
                        ? directProducts
                        : default;
                return mapper.MapToSummaryList(products);
            },
            ct);
    }
}
