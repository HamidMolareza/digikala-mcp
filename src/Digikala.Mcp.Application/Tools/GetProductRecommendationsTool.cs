using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductRecommendationsTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("get_product_recommendations"), Description("Get recommended/similar products for a given product.")]
    public async Task<List<ProductSummaryDto>> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        [Description("Offset for pagination, starting from 0")] int offset = 0,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
