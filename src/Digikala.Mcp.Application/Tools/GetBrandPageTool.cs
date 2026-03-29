using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetBrandPageTool(IDigikalaApiClient apiClient, ICacheStore cache, BrandMapper mapper)
{
    [McpServerTool, Description("Get a premium brand landing page.")]
    public async Task<BrandPageDto> get_brand_page(
        [Description("Brand slug, e.g. 'persil'")] string brandSlug,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_brand_page", ("brand", brandSlug));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = await apiClient.GetBrandPremiumAsync(brandSlug, token);
                return mapper.MapToBrandPage(response);
            },
            ct);
    }
}
