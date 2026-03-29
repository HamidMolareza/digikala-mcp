using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetBestSellingService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<List<ProductSummaryDto>> ExecuteAsync(
        int? categoryId = null,
        string? lastDays = null,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_best_selling", ("categoryId", categoryId), ("lastDays", lastDays));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = await apiClient.GetBestSellingAsync(categoryId, lastDays, token);
                var data = JsonHelpers.GetData(response);
                return mapper.MapToSummaryList(data.TryGetNested(out var products, "products") ? products : default);
            },
            skipCache,
            ct);
    }
}
