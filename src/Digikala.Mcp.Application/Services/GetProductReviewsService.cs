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
        var request = new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["page"] = page.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_reviews", ("productId", productId), ("page", page));
        try
        {
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
        catch (Exception exception)
        {
            return new ReviewListDto
            {
                CurrentPage = page,
                Warning = "Digikala product reviews request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_product_reviews",
                    "Digikala product reviews request failed.",
                    exception,
                    request),
            };
        }
    }
}
