using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class SearchProductsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("search_products"), Description("Search products by keyword within an optional category, with filters, sorting, and pagination.")]
    public async Task<SearchResultDto> ExecuteAsync(
        [Description("Search keyword in Persian or English")] string query,
        [Description("Category slug, e.g. 'mobile-phone'. Null for all categories.")] string? categorySlug = null,
        [Description("Page number, starting from 1")] int page = 1,
        [Description("Sort: 1=relevance, 4=popular, 7=cheapest, 8=expensive, 25=best-selling, 27=newest")] int? sort = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
