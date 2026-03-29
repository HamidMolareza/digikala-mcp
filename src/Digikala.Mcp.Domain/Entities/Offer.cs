namespace Digikala.Mcp.Domain.Entities;

public sealed class Offer
{
    public long ProductId { get; init; }
    public string TitleFa { get; init; } = string.Empty;
    public long SellingPriceRials { get; init; }
    public long? OriginalPriceRials { get; init; }
    public int? DiscountPercent { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
