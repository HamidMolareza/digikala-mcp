namespace Digikala.Mcp.Application.Dtos;

public sealed record OfferDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public long ProductId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? MainImageUrl { get; init; }
    public long SellingPriceRials { get; init; }
    public long? OriginalPriceRials { get; init; }
    public int? DiscountPercent { get; init; }
    public string Url { get; init; } = string.Empty;
}

public sealed record OffersListDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public List<OfferDto> Offers { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
}
