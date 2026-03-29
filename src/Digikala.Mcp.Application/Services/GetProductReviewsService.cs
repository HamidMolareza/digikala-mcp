using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductReviewsService(IDigikalaApiClient apiClient, ICacheStore cache, ReviewMapper mapper)
{
    public async Task<ReviewListDto> ExecuteAsync(
        long productId,
        int page = 1,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_reviews", ("productId", productId), ("page", page));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductCommentsAsync(productId, page, ct: token);
                return mapper.MapToReviewList(response);
            },
            skipCache,
            ct);
    }
}
