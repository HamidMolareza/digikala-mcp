using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetCategoryLandingService(IDigikalaApiClient apiClient, ICacheStore cache, CategoryMapper mapper)
{
    public async Task<CategoryDto> ExecuteAsync(
        string categorySlug,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_category_landing", ("category", categorySlug));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.LongTerm,
            async token =>
            {
                using var response = await apiClient.GetCategoryLandingAsync(categorySlug, token);
                return mapper.MapToCategory(response);
            },
            skipCache,
            ct);
    }
}
