using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductPriceChartTool(IDigikalaApiClient apiClient, ICacheStore cache, PriceChartMapper mapper)
{
    [McpServerTool("get_product_price_chart"), Description("Get historical price data for a product.")]
    public async Task<PriceChartDto> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
