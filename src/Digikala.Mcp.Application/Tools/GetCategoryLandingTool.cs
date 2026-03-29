using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetCategoryLandingTool(IDigikalaApiClient apiClient, ICacheStore cache, CategoryMapper mapper)
{
    [McpServerTool("get_category_landing"), Description("Get a category landing page with subcategories, popular brands, and highlights.")]
    public async Task<CategoryDto> ExecuteAsync(
        [Description("Category slug, e.g. 'electronic-devices'")] string categorySlug,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
