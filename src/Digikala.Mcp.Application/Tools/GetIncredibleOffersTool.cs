using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetIncredibleOffersTool(IDigikalaApiClient apiClient, ICacheStore cache, OfferMapper mapper)
{
    [McpServerTool, Description("Get products currently on incredible/lightning deals.")]
    public async Task<OffersListDto> get_incredible_offers(
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_incredible_offers", ("page", page));
        return await ToolExecutionHelper.GetOrCreateAsync(
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
