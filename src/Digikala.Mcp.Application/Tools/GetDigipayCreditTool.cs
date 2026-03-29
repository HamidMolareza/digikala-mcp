using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetDigipayCreditTool(IDigikalaApiClient apiClient, ICacheStore cache)
{
    [McpServerTool("get_digipay_credit"), Description("Get Digipay credit/installment options for a product.")]
    public async Task<DigipayCreditDto> ExecuteAsync(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
