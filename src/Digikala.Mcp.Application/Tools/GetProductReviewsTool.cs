using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductReviewsTool(IDigikalaApiClient apiClient, ICacheStore cache, ReviewMapper mapper)
{
    [McpServerTool, Description("Get paginated user reviews for a product.")]
    public async Task<ReviewListDto> get_product_reviews(
        [Description("Numeric product ID")] long productId,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_reviews", ("productId", productId), ("page", page));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductCommentsAsync(productId, page, ct: token);
                return mapper.MapToReviewList(response);
            },
            ct);
    }
}
