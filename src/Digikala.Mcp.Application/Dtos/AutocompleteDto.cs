namespace Digikala.Mcp.Application.Dtos;

public sealed record AutocompleteDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public List<SuggestionDto> Suggestions { get; init; } = [];
    public List<string> TrendingSearches { get; init; } = [];
}

public sealed record SuggestionDto
{
    public string Text { get; init; } = string.Empty;
    public string? CategorySlug { get; init; }
    public string? Url { get; init; }
}
