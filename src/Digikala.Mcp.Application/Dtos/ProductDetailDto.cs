namespace Digikala.Mcp.Application.Dtos;

public sealed record ProductDetailDto : ProductSummaryDto
{
    public ToolIssueDto? Error { get; set; }
    public string? Description { get; init; }
    public string? Brand { get; init; }
    public string? BrandSlug { get; init; }
    public string? Seller { get; init; }
    public double? SellerRating { get; init; }
    public string? Warranty { get; init; }
    public List<SpecDto> Specifications { get; init; } = [];
    public List<string> ImageUrls { get; init; } = [];
    public List<VariantDto> Variants { get; init; } = [];
    public ProsConsDto? ProsCons { get; init; }
    public BuyerCommentsSummaryDto? BuyerCommentsSummary { get; init; }
}

public sealed record SpecDto(string GroupTitle, string Title, string Value);

public sealed record VariantDto
{
    public long Id { get; init; }
    public string? Title { get; init; }
    public long PriceRials { get; init; }
    public bool IsAvailable { get; init; }
    public string? ColorHex { get; init; }
}

public sealed record ProsConsDto(List<string> Pros, List<string> Cons);

public sealed record BuyerCommentsSummaryDto
{
    public long Id { get; init; }
    public string Summary { get; init; } = string.Empty;
    public List<string> Pros { get; init; } = [];
    public List<string> Cons { get; init; } = [];
}
