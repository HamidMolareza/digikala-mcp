namespace Digikala.Mcp.Application.Dtos;

public sealed record CategoryDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Url { get; init; }
    public List<SubCategoryDto> SubCategories { get; init; } = [];
    public List<ProductSummaryDto> BestSellingProducts { get; init; } = [];
    public List<BrandSummaryDto> PopularBrands { get; init; } = [];
}

public sealed record SubCategoryDto(int Id, string Title, string Slug);

public sealed record BrandSummaryDto(int Id, string Title, string Slug, string? LogoUrl);
