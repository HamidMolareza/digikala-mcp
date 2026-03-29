using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class BrowseByTagService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<SearchResultDto> ExecuteAsync(
        string tagSlug,
        int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("browse_by_tag", ("tag", tagSlug), ("page", page));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = await apiClient.GetTagProductsAsync(tagSlug, page, token);
                return mapper.MapToSearchResult(response);
            },
            ct);
    }
}
