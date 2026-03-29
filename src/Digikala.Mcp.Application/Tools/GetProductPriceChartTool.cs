using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductPriceChartTool(IDigikalaApiClient apiClient, ICacheStore cache, PriceChartMapper mapper)
{
    [McpServerTool, Description("Get historical price data for a product.")]
    public async Task<PriceChartDto> get_product_price_chart(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_price_chart", ("productId", productId));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductPriceChartAsync(productId, ct: token);
                return mapper.MapToPriceChart(productId, response);
            },
            ct);
    }
}
