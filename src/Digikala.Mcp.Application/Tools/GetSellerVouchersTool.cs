using System.ComponentModel;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetSellerVouchersTool(IDigikalaApiClient apiClient, ICacheStore cache)
{
    [McpServerTool, Description("Get seller vouchers/promotions for a product.")]
    public async Task<List<SellerVoucherDto>> get_seller_vouchers(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_seller_vouchers", ("productId", productId));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductSellerVouchersAsync(productId, token);
                var data = JsonHelpers.GetData(response);
                return data.EnumerateArrayOrEmpty("vouchers")
                    .Select(item => new SellerVoucherDto
                    {
                        Title = item.GetStringOrNull("title") ?? string.Empty,
                        Description = item.GetStringOrNull("description"),
                        MinPurchaseRials = item.GetInt64OrNull("min_purchase"),
                        DiscountPercent = item.GetInt32OrNull("discount_percent"),
                        MaxDiscountRials = item.GetInt64OrNull("max_discount"),
                    })
                    .ToList();
            },
            ct);
    }
}
