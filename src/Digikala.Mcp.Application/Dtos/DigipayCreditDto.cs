namespace Digikala.Mcp.Application.Dtos;

public sealed record DigipayCreditDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public string? Url { get; init; }
    public List<CreditItemDto> Items { get; init; } = [];
}

public sealed record CreditItemDto
{
    public string Title { get; init; } = string.Empty;
    public long MonthlyPaymentRials { get; init; }
    public int Months { get; init; }
}
