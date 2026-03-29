using System.ComponentModel;
using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;
using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.Interfaces;
using ModelContextProtocol.Server;

namespace Digikala.Mcp.Application.Tools;

[McpServerToolType]
public sealed class GetDigipayCreditTool(IDigikalaApiClient apiClient, ICacheStore cache)
{
    [McpServerTool, Description("Get Digipay credit/installment options for a product.")]
    public async Task<DigipayCreditDto> get_digipay_credit(
        [Description("Numeric product ID")] long productId,
        CancellationToken ct = default)
    {
        var cacheKey = ToolExecutionHelper.CreateCacheKey("get_digipay_credit", ("productId", productId));
        return await ToolExecutionHelper.GetOrCreateAsync(
            cache,
            cacheKey,
            CacheTier.ShortTerm,
            async token =>
            {
                using var response = await apiClient.GetProductDigipayCreditAsync(productId, token);
                var data = JsonHelpers.GetData(response);
                return new DigipayCreditDto
                {
                    Url = data.GetStringOrNull("url") ?? data.GetUrl("url"),
                    Items = data.EnumerateArrayOrEmpty("items")
                        .Select(item => new CreditItemDto
                        {
                            Title = item.GetStringOrNull("title") ?? string.Empty,
                            MonthlyPaymentRials = item.GetInt64OrNull("monthly_payment") ?? 0,
                            Months = item.GetInt32OrNull("months") ?? 0,
                        })
                        .ToList(),
                };
            },
            ct);
    }
}
