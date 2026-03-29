using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductDetailTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("get_product_detail"), Description("Get full product detail including variants, images, price, seller, and specs.")]
    public async Task<ProductDetailDto> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        [Description("Warehouse ID for Fresh products. Null for main store.")] int? warehouseId = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
