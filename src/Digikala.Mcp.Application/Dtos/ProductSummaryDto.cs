using System.ComponentModel;

namespace Digikala.Mcp.Application.Dtos;

public record ProductSummaryDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? EnglishTitle { get; init; }
    public string? MainImageUrl { get; init; }
    public long PriceRials { get; init; }
    public long? OriginalPriceRials { get; init; }
    public int? DiscountPercent { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public string? CategorySlug { get; init; }
    public bool IsAvailable { get; init; }
    public string Url { get; init; } = string.Empty;
}
