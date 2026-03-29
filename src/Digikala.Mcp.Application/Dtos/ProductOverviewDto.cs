namespace Digikala.Mcp.Application.Dtos;

public sealed record ProductOverviewDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public bool HasError { get; set; }
    public string? Warning { get; set; }
    public List<string> SectionWarnings { get; init; } = [];
    public Dictionary<string, string> SectionStatus { get; init; } = [];
    public Dictionary<string, ToolIssueDto> SectionIssues { get; init; } = [];
    public ProductDetailDto Detail { get; init; } = new();
    public PriceChartDto? PriceChart { get; init; }
    public List<ReviewDto> TopReviews { get; init; } = [];
    public List<QuestionDto> TopQuestions { get; init; } = [];
    public List<ProductSummaryDto> Recommendations { get; init; } = [];
}
