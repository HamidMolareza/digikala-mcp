using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Application.Mapping;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductDetailService(IDigikalaApiClient apiClient, ICacheStore cache, ProductMapper mapper)
{
    public async Task<ProductDetailDto> ExecuteAsync(
        long productId,
        int? warehouseId = null,
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var request = new Dictionary<string, string?>
        {
            ["productId"] = productId.ToString(),
            ["warehouseId"] = warehouseId?.ToString(),
            ["skipCache"] = skipCache.ToString(),
        };
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_product_detail", ("productId", productId), ("warehouseId", warehouseId));
        try
        {
            return await ToolExecutionHelper.GetOrCreateWithStaleFallbackAsync(
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
                skipCache,
                ct);
        }
        catch (Exception exception)
        {
            return new ProductDetailDto
            {
                Id = productId,
                Warning = "Digikala product detail request failed; returned structured diagnostics instead of an opaque tool failure.",
                Error = ToolIssueFactory.Create(
                    "get_product_detail",
                    "Digikala product detail request failed.",
                    exception,
                    request),
            };
        }
    }
}
