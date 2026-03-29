namespace Digikala.Mcp.Application.Dtos;

public sealed record ProductNeedRecommendationsDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public string SearchQuery { get; init; } = string.Empty;
    public NeedQueryUnderstandingDto QueryUnderstanding { get; init; } = new();
    public List<ProductNeedRecommendationDto> Recommendations { get; init; } = [];
}

public sealed record NeedQueryUnderstandingDto
{
    public List<string> RequiredSignals { get; init; } = [];
    public List<string> PreferredSignals { get; init; } = [];
    public List<string> AvoidSignals { get; init; } = [];
}

public sealed record ProductNeedRecommendationDto
{
    public ProductSummaryDto Product { get; init; } = new();
    public double Score { get; init; }
    public List<string> Why { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public ProductNeedRecommendationEvidenceDto? Evidence { get; init; }
}

public sealed record ProductNeedRecommendationEvidenceDto
{
    public List<string> MatchedSignals { get; init; } = [];
    public List<string> ReviewSignals { get; init; } = [];
    public string? FeedbackSummary { get; init; }
    public List<string> FeedbackSnippets { get; init; } = [];
}
