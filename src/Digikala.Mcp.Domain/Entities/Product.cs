using Digikala.Mcp.Domain.Enums;
using Digikala.Mcp.Domain.ValueObjects;

namespace Digikala.Mcp.Domain.Entities;

public sealed class Product
{
    public long Id { get; init; }
    public string TitleFa { get; init; } = string.Empty;
    public string? TitleEn { get; init; }
    public string? CategorySlug { get; init; }
    public string Url { get; init; } = string.Empty;
    public List<ImageUrl> Images { get; init; } = [];
    public Money? SellingPrice { get; init; }
    public Money? OriginalPrice { get; init; }
    public Rating? Rating { get; init; }
    public ProductAvailability Availability { get; init; }
}
