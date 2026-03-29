namespace Digikala.Mcp.Application.Dtos;

public sealed record ProductFeedbackSummaryDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public long ProductId { get; init; }
    public List<FeedbackCountDto> Pros { get; init; } = [];
    public List<FeedbackCountDto> Cons { get; init; } = [];
    public List<FeedbackThemeDto> Themes { get; init; } = [];
    public string Summary { get; init; } = string.Empty;
}

public sealed record FeedbackCountDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record FeedbackThemeDto
{
    public string Name { get; init; } = string.Empty;
    public string Signal { get; init; } = string.Empty;
    public int EvidenceCount { get; init; }
    public List<string> SampleSnippets { get; init; } = [];
}
