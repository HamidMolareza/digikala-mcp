using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetTrendingProductsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Get trending products.")]
    public async Task<List<ProductSummaryDto>> get_trending_products(CancellationToken ct = default)
    {
        const string cacheKey = "get_trending_products";
        return await ToolExecutionHelper.GetOrCreateAsync(
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
