using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetIncredibleOffersService(IDigikalaApiClient apiClient, ICacheStore cache, OfferMapper mapper)
{
    public async Task<OffersListDto> ExecuteAsync(
        int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_incredible_offers", ("page", page));
        return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = page <= 1
                    ? await apiClient.GetIncredibleOffersAsync(token)
                    : await apiClient.GetIncredibleOffersProductsAsync(page, ct: token);
                return mapper.MapToOffersList(response);
            },
            ct);
    }
}
