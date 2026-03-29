using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetSellerVouchersTool(IDigikalaApiClient apiClient, ICacheStore cache)
{
    [McpServerTool("get_seller_vouchers"), Description("Get seller vouchers/promotions for a product.")]
    public async Task<List<SellerVoucherDto>> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
