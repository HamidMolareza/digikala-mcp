using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetIncredibleOffersTool(IDigikalaApiClient apiClient, ICacheStore cache, OfferMapper mapper)
{
    [McpServerTool("get_incredible_offers"), Description("Get products currently on incredible/lightning deals.")]
    public async Task<OffersListDto> ExecuteAsync(
        [Description("Page number, starting from 1")] int page = 1,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
