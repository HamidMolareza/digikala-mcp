using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetBestSellingTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Get best-selling products, optionally by category and time window.")]
    public async Task<List<ProductSummaryDto>> get_best_selling(
        [Description("Numeric category ID. Null for all.")] int? categoryId = null,
        [Description("Time window filter, e.g. 'month'")] string? lastDays = null,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_best_selling", ("categoryId", categoryId), ("lastDays", lastDays));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = await apiClient.GetBestSellingAsync(categoryId, lastDays, token);
                var data = JsonHelpers.GetData(response);
                return mapper.MapToSummaryList(data.TryGetNested(out var products, "products") ? products : default);
            },
            ct);
    }
}
