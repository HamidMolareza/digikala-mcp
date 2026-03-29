using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetCategoryLandingTool(IDigikalaApiClient apiClient, ICacheStore cache, CategoryMapper mapper)
{
    [McpServerTool, Description("Get a category landing page with subcategories, popular brands, and highlights.")]
    public async Task<CategoryDto> get_category_landing(
        [Description("Category slug, e.g. 'electronic-devices'")] string categorySlug,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_category_landing", ("category", categorySlug));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.LongTerm,
            async token =>
            {
                using var response = await apiClient.GetCategoryLandingAsync(categorySlug, token);
                return mapper.MapToCategory(response);
            },
            ct);
    }
}
