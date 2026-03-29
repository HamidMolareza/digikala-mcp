namespace Digikala.Mcp.Application.Dtos;

public sealed record PriceChartDto
{
    public long ProductId { get; init; }
    public long CurrentPriceRials { get; init; }
    public long? LowestPriceRials { get; init; }
    public long? HighestPriceRials { get; init; }
    public List<PricePointDto> History { get; init; } = [];
}

public sealed record PricePointDto(DateOnly Date, long PriceRials);
