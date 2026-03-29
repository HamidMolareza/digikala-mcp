using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductRecommendationsService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<List<ProductSummaryDto>> ExecuteAsync(
        long productId,
        int offset = 0,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_recommendations", ("productId", productId), ("offset", offset));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = await apiClient.GetProductRecommendationAsync(productId, offset, token);
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
