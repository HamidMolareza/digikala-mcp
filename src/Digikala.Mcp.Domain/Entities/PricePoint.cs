namespace Digikala.Mcp.Domain.Entities;

public sealed class PricePoint
{
    public DateOnly Date { get; init; }
    public long PriceRials { get; init; }
}
