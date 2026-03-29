namespace Digikala.Mcp.Application.Dtos;

public sealed record SellerVoucherDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? MinPurchaseRials { get; init; }
    public int? DiscountPercent { get; init; }
    public long? MaxDiscountRials { get; init; }
}
