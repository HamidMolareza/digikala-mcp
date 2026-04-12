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
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["brandSlug"] = brandSlug,
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_brand_page", ("brand", brandSlug));
        try
        {
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
                cache,
                cacheKey,
                CacheTier.MediumTerm,
                async token =>
                {
                    using var response = await apiClient.GetBrandPremiumAsync(brandSlug, token);
                    return mapper.MapToBrandPage(response);
                },
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new BrandPageDto
            {
                Warning = "Digikala brand page request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_brand_page",
                    "Digikala brand page request failed.",
                    exception,
                    request),
            };
        }
    }
}
