using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class BrowseByTagTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Browse products by SEO tag (e.g. 'football-boots', 'iphone-16').")]
    public async Task<SearchResultDto> browse_by_tag(
        [Description("Tag slug, e.g. 'iphone-16'")] string tagSlug,
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("browse_by_tag", ("tag", tagSlug), ("page", page));
        return await ToolExecutionHelper.GetOrCreateAsync(
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
