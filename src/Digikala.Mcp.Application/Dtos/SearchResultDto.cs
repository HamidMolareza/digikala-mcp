namespace Digikala.Mcp.Application.Dtos;

public sealed record SearchResultDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public List<ProductSummaryDto> Products { get; init; } = [];
    public ProductSummaryDto? BestMatch { get; set; }
    public int TotalCount { get; init; }
    public int FilteredCount { get; set; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public List<string> AppliedFilters { get; set; } = [];
    public List<FilterGroupDto> AvailableFilters { get; init; } = [];
    public List<string> AvailableSortOptions { get; init; } = [];
}

public sealed record FilterGroupDto
{
    public string Title { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public List<FilterOptionDto> Options { get; init; } = [];
}

public sealed record FilterOptionDto
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Count { get; init; }
}
