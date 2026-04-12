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
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_incredible_offers", ("page", page));
        try
        {
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
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new OffersListDto
            {
                Warning = "Digikala incredible offers request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_incredible_offers",
                    "Digikala incredible offers request failed.",
                    exception,
                    request),
            };
        }
    }
}
