namespace Digikala.Mcp.Application.Dtos;

public sealed record ProductOverviewDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public ProductDetailDto Detail { get; init; } = new();
    public PriceChartDto? PriceChart { get; init; }
    public List<ReviewDto> TopReviews { get; init; } = [];
    public List<QuestionDto> TopQuestions { get; init; } = [];
    public List<ProductSummaryDto> Recommendations { get; init; } = [];
}
