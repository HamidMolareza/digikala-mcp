using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetBestSellingTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool("get_best_selling"), Description("Get best-selling products, optionally by category and time window.")]
    public async Task<List<ProductSummaryDto>> ExecuteAsync(
        [Description("Numeric category ID. Null for all.")] int? categoryId = null,
        [Description("Time window filter, e.g. 'month'")] string? lastDays = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
