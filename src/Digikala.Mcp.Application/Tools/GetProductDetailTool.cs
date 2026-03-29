using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetProductDetailTool(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    [McpServerTool, Description("Get full product detail including variants, images, price, seller, and specs.")]
    public async Task<ProductDetailDto> get_product_detail(
        [Description("Numeric product ID")] long productId,
        [Description("Warehouse ID for Fresh products. Null for main store.")] int? warehouseId = null,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_detail", ("productId", productId), ("warehouseId", warehouseId));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.MediumTerm,
            async token =>
            {
                using var response = warehouseId is null
                    ? await apiClient.GetProductV2Async(productId, ct: token)
                    : await apiClient.GetFreshProductAsync(productId, warehouseId.Value, token);
                var data = JsonHelpers.GetData(response);
                return mapper.MapToDetail(data.TryGetNested(out var product, "product") ? product : data);
            },
            ct);
    }
}
