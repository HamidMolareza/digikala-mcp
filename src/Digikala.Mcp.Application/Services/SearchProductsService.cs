using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class SearchProductsService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<SearchResultDto> ExecuteAsync(
        string query,
        string? categorySlug = null,
        int page = 1,
        int? sort = null,
        CancellationToken ct = default)
    {
        var normalizedCategorySlug = string.IsNullOrWhiteSpace(categorySlug) ? "search" : categorySlug.Trim();
        var cacheKey = ToolExecutionHelper.CreateCacheKey(
            "search_products",
            ("query", query),
            ("category", normalizedCategorySlug),
            ("page", page),
            ("sort", sort));

        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.SearchCategoryAsync(
                    normalizedCategorySlug,
                    page,
                    sort,
                    query: query,
                    ct: token);

                return mapper.MapToSearchResult(response);
            },
            ct);
    }
}
