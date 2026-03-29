using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetBrandPageService(IDigikalaApiClient apiClient, ICacheStore cache, BrandMapper mapper)
{
    public async Task<BrandPageDto> ExecuteAsync(
        string brandSlug,
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
