namespace Digikala.Mcp.Domain.ValueObjects;

public sealed record Money(long AmountRials)
{
    public decimal ToToman() => AmountRials / 10m;
}
