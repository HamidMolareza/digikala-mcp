namespace Digikala.Mcp.Application.Dtos;

public sealed record BrandPageDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public List<ProductSummaryDto> NewestProducts { get; init; } = [];
    public List<ProductSummaryDto> BestSellingProducts { get; init; } = [];
}
