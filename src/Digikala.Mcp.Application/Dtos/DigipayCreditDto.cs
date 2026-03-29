namespace Digikala.Mcp.Application.Dtos;

public sealed record DigipayCreditDto
{
    public string? Url { get; init; }
    public List<CreditItemDto> Items { get; init; } = [];
}

public sealed record CreditItemDto
{
    public string Title { get; init; } = string.Empty;
    public long MonthlyPaymentRials { get; init; }
    public int Months { get; init; }
}
