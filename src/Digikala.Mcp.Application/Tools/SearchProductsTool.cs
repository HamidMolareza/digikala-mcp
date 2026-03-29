using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class SearchProductsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Search products by keyword within an optional category, with filters, sorting, and pagination.")]
    public async Task<SearchResultDto> search_products(
        [Description("Search keyword in Persian or English")] string query,
        [Description("Category slug, e.g. 'mobile-phone'. Null for all categories.")] string? categorySlug = null,
        [Description("Page number, starting from 1")] int page = 1,
        [Description("Sort: 1=relevance, 4=popular, 7=cheapest, 8=expensive, 25=best-selling, 27=newest")] int? sort = null,
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
