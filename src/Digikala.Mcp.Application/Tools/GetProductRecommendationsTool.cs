using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductRecommendationsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Get recommended/similar products for a given product.")]
    public async Task<List<ProductSummaryDto>> get_product_recommendations(
        [Description("Numeric product ID")] long productId,
        [Description("Offset for pagination, starting from 0")] int offset = 0,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_recommendations", ("productId", productId), ("offset", offset));
        return await ToolExecutionHelper.GetOrCreateAsync(
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
