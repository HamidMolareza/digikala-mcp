using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetTrendingProductsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("get_trending_products"), Description("Get trending products.")]
    public async Task<List<ProductSummaryDto>> ExecuteAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
