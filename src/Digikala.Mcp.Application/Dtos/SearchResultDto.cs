namespace Digikala.Mcp.Application.Dtos;

public sealed record SearchResultDto
{
    public List<ProductSummaryDto> Products { get; init; } = [];
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
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
