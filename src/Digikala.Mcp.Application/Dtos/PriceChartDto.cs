namespace Digikala.Mcp.Application.Dtos;

public sealed record PriceChartDto
{
    public bool IsStale { get; set; }
    public bool IsFromCache { get; set; }
    public string CacheStatus { get; set; } = "unknown";
    public string? Warning { get; set; }
    public ToolIssueDto? Error { get; set; }
    public long ProductId { get; init; }
    public long CurrentPriceRials { get; init; }
    public long? LowestPriceRials { get; init; }
    public long? HighestPriceRials { get; init; }
    public List<PricePointDto> History { get; init; } = [];
}

public sealed record PricePointDto(DateOnly Date, long PriceRials);
