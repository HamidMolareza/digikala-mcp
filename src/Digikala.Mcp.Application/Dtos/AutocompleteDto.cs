namespace Digikala.Mcp.Application.Dtos;

public sealed record AutocompleteDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public List<SuggestionDto> Suggestions { get; init; } = [];
    public List<string> TrendingSearches { get; init; } = [];
}

public sealed record SuggestionDto
{
    public string Text { get; init; } = string.Empty;
    /// <summary>"query" or "category".</summary>
    public string? Type { get; init; }
    public string? CategoryTitle { get; init; }
    public string? CategorySlug { get; init; }
    public string? Url { get; init; }
}
