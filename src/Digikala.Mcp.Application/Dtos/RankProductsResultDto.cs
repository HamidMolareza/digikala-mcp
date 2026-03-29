namespace Digikala.Mcp.Application.Dtos;

public sealed record RankProductsResultDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public string SearchQuery { get; init; } = string.Empty;
    public string? CategorySlug { get; init; }
    public int CandidateCount { get; init; }
    public List<string> AppliedRankingPriority { get; init; } = [];
    public List<string> AppliedFilters { get; init; } = [];
    public List<RankedProductDto> Products { get; init; } = [];
}

public sealed record RankedProductDto
{
    public int Rank { get; init; }
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public long PriceRials { get; init; }
    public string Url { get; init; } = string.Empty;
    public string AbsoluteUrl { get; init; } = string.Empty;
    public double? Rating { get; init; }
    public int RatingCount { get; init; }
    public int ReviewCount { get; init; }
    public double? SearchScore { get; init; }
    public List<string> WhyRankedHere { get; init; } = [];
}
